package gr.jimmys.jimmysfoodzilla.services;

import com.nimbusds.jose.jwk.JWKSet;
import gr.jimmys.jimmysfoodzilla.controllers.DishController;
import gr.jimmys.jimmysfoodzilla.models.Token;
import gr.jimmys.jimmysfoodzilla.repository.TokenRepository;
import gr.jimmys.jimmysfoodzilla.utils.Tuple3;
import kong.unirest.HttpResponse;
import kong.unirest.JsonNode;
import kong.unirest.Unirest;
import kong.unirest.UnirestException;
import kong.unirest.json.JSONObject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.net.URI;
import java.time.Duration;
import java.time.LocalDateTime;
import java.util.List;

@Service
public class JwtValidationAndRenewalService {
    private final TokenRepository tokenRepository;
    private String managementApiAccessTokenValue;
    @Value("${auth0.domain}")
    private String auth0_domain;

    @Value("${auth0.m2m_clientid}")
    private String auth0_m2m_clientid;

    @Value("${auth0.m2m_clientsecret}")
    private String auth0_m2m_clientsecret;

    public synchronized JWKSet getJwkSet() {
        return jwkSet;
    }

    public synchronized void setJwkSet(JWKSet jwkSet) {
        this.jwkSet = jwkSet;
    }

    private JWKSet jwkSet; //used on "CreateUser" controller
    private final Logger logger = LoggerFactory.getLogger(DishController.class);
    private Tuple3<Boolean, LocalDateTime, String> isTokenExpired() {
        List<Token> tokenFromDb = tokenRepository.findAllManagementApiTokens("M_API");
        if (tokenFromDb.size() == 0) {
            logger.info("JwtValidationAndRenewalService: No ManagementAPI Token found in db, fetching new...");
            return new Tuple3<Boolean, LocalDateTime, String>(true, null, "");
        }
        return new Tuple3<Boolean, LocalDateTime, String>(
                tokenFromDb.get(0).getExpiryDate().compareTo(LocalDateTime.now()) <= 0,
                tokenFromDb.get(0).getExpiryDate(),
                tokenFromDb.get(0).getTokenValue());
    }

    private Tuple3<LocalDateTime, Boolean, String> renewToken() {
        try {
            HttpResponse<JsonNode> response = Unirest.post("https://" + auth0_domain + "/oauth/token")
                    .header("content-type", "application/x-www-form-urlencoded")
                    .body("grant_type=client_credentials&client_id=" + auth0_m2m_clientid + "&client_secret=" + auth0_m2m_clientsecret + "&audience=https%3A%2F%2F" + auth0_domain + "%2Fapi%2Fv2%2F")
                    .asJson();
            if (response.getStatus() != 200) {
                logger.error("JwtValidationAndRenewalService: Error in ManagementAPI oauth/token HTTP POST request, could not receive token\n" +
                        "Reason: STATUS CODE: " + response.getStatus() + " STATUS TEXT: " + response.getStatusText());
                return new Tuple3<LocalDateTime, Boolean, String>(LocalDateTime.now(), false, "");
            }
            // Parse the JSON response
            JsonNode jsonNode = response.getBody();
            JSONObject jsonObject = jsonNode.getObject();

            // Extract data
            String accessToken = jsonObject.getString("access_token");
            int expiresIn = jsonObject.getInt("expires_in");
            String scope = jsonObject.getString("scope");
            String tokenType = jsonObject.getString("token_type");

            if (accessToken == null || expiresIn == 0 || scope == null || tokenType == null) {
                logger.error("JwtValidationAndRenewalService: ManagementAPI token is malformed! Check Auth0 configuration");
                return new Tuple3<LocalDateTime, Boolean, String>(LocalDateTime.now(), false, "");
            }

            List<Token> tokensFromDb = tokenRepository.findAllManagementApiTokens("M_API");
            if (tokensFromDb.size() > 0)
                tokenRepository.delete(tokensFromDb.get(0));

            //create the new Token entity
            LocalDateTime tokenExpirationDate = LocalDateTime.now().plusSeconds(expiresIn);
            Token newToken = new Token(accessToken, "M_API", tokenExpirationDate);

            //save to db
            tokenRepository.save(newToken);
            logger.info("JwtValidationAndRenewalService: Auth0 Management API Token successfully saved");

            return new Tuple3<>(tokenExpirationDate, true, accessToken);

        } catch (UnirestException e) {
           logger.error("JwtValidationAndRenewalService: Error in ManagementAPI oauth/token HTTP POST request, could not receive token");
           return new Tuple3<LocalDateTime, Boolean, String>(LocalDateTime.now(), false, "");
        }
    }

    public JwtValidationAndRenewalService(TokenRepository tokenRepository) {
        this.tokenRepository = tokenRepository;
        setManagementApiAccessTokenValue("");

        Thread jwtValidationAndRenewalThread = new Thread(() -> {
            logger.info("JwtValidationAndRenewalService: START Service");
            try {
                Thread.sleep(5000); //wait so intiialization happens
                setJwkSet(JWKSet.load(new URI("https://" + auth0_domain+ "/.well-known/jwks.json").toURL()));
                logger.info("JwtValidationAndRenewalService: RENEWED jwks.json");
            } catch (Exception e) {
                logger.error("JwtValidationAndRenewalService: COULD NOT RENEW jwks.json");
                setJwkSet(null);
            }
            while (true) {
                try {
                    Tuple3<Boolean, LocalDateTime, String> tokenExpiredValues = isTokenExpired();
                    //check if token is expired
                    if (tokenExpiredValues.getFirst()) {
                        //renew the token and get the expiration time
                        Tuple3<LocalDateTime, Boolean, String> renewTokenValues = renewToken();
                        //something bad happened while renewing (network error etc) -> wait some seconds and try again later
                        if (!renewTokenValues.getSecond()) {
                            Thread.sleep(20 * 1000);
                        } else {
                            //calculate the time to sleep (minus 30 seconds)
                            LocalDateTime tokenExpiration = renewTokenValues.getFirst();
                            Duration sleepTime = Duration.between(LocalDateTime.now(), tokenExpiration.minusSeconds(30));
                            setManagementApiAccessTokenValue(renewTokenValues.getThird());
                            //sleep util it's time to renew the token (plus some seconds)
                            Thread.sleep(sleepTime.toMillis());
                        }
                    }
                    //not expired
                    else {
                        //the token is still valid
                        setManagementApiAccessTokenValue(tokenExpiredValues.getThird());
                        Duration sleepTime = Duration.between(LocalDateTime.now(), tokenExpiredValues.getSecond().minusSeconds(30));
                        if (!sleepTime.isNegative() && !sleepTime.isZero())
                            Thread.sleep(sleepTime.toMillis());
                    }
                } catch (InterruptedException ie) {
                    logger.error("JwtValidationAndRenewalService: Background thread INTERRUPTED! New M2M API Tokens won't be received!");
                }
            }
        });
        jwtValidationAndRenewalThread.start();
    }

    public synchronized String getManagementApiAccessTokenValue() {
        return managementApiAccessTokenValue;
    }
    public synchronized void setManagementApiAccessTokenValue(String value) {
        this.managementApiAccessTokenValue = value;
    }
}
