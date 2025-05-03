package gr.jimmys.jimmysfoodzilla.services.impl;

import com.nimbusds.jose.JOSEException;
import com.nimbusds.jose.crypto.RSASSAVerifier;
import com.nimbusds.jose.jwk.JWKSet;
import com.nimbusds.jwt.SignedJWT;
import gr.jimmys.jimmysfoodzilla.services.api.JwtRenewalService;
import gr.jimmys.jimmysfoodzilla.services.api.JwtService;
import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;

import java.net.URI;
import java.text.ParseException;
import java.time.Duration;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

@Service
public class JwtRenewalServiceImpl implements JwtRenewalService {
    private final Logger logger = LoggerFactory.getLogger(JwtRenewalServiceImpl.class);

    @Autowired
    private JwtService jwtService;

    @Value("${auth0.domain}")
    private String domain;

    private String managementApiToken;

    private JWKSet jwkSet;

    public JwtRenewalServiceImpl() {
        setManagementApiToken("");
    }

    @PostConstruct
    public void init() {
        Thread jwtValidationAndRenewalThread = new Thread(() -> {
            logger.info("START Service");
            try {
                setJwkSet(JWKSet.load(new URI("https://" + domain + "/.well-known/jwks.json").toURL()));
                logger.info("RENEWED jwks.json");
            } catch (Exception e) {
                logger.error("COULD NOT RENEW jwks.json");
                setJwkSet(null);
            }
            logger.info("Renew token main loop started");
            while (true) {
                try {
                    logger.info("Retrieving local token...");
                    var token = jwtService.retrieveToken();
                    //check if token is expired
                    if (token == null || token.getExpiryDate().isBefore(LocalDateTime.now())) {
                        logger.info("No token found in db, or it is expired, renewing...");
                        //renew the token and get the expiration time
                        var newAccessToken = jwtService.renewToken();
                        //something bad happened while renewing (network error etc) -> wait some seconds and try again later
                        if (newAccessToken == null) {
                            Thread.sleep(20 * 1000);
                            continue;
                        }
                        //renew token and sleep until it's time to renew the token again
                        var tokenExpiration = newAccessToken.getExpiryDate();
                        var sleepTime = Duration.between(LocalDateTime.now(), tokenExpiration.minusSeconds(30));
                        setManagementApiToken(newAccessToken.getTokenValue());
                        logger.info("Successfully renewed token");
                        //sleep util it's time to renew the token (plus some seconds)
                        Thread.sleep(sleepTime.toMillis());

                    } else {
                        //the token is still valid
                        logger.info("Successfully retrieved local token. It will expire at: {}", token.getExpiryDate().format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm")));
                        setManagementApiToken(token.getTokenValue());
                        var sleepTime = Duration.between(LocalDateTime.now(), token.getExpiryDate().minusSeconds(30));
                        if (!sleepTime.isNegative() && !sleepTime.isZero())
                            Thread.sleep(sleepTime.toMillis());
                    }
                } catch (InterruptedException ie) {
                    logger.error("Background thread INTERRUPTED! New M2M API Tokens won't be received!");
                }
            }
        });
        jwtValidationAndRenewalThread.start();
    }

    @Override
    public HttpStatus validateToken(String token, String audienceToCheck) {
        try {
            if (jwkSet == null || audienceToCheck == null) //internal problem...
                return HttpStatus.INTERNAL_SERVER_ERROR;
            if (token == null || !token.startsWith("Bearer "))
                return HttpStatus.BAD_REQUEST; //bad token value (avoid substring crash)
            // Extract the JWT token
            var signedJWT = SignedJWT.parse(token.substring(7)); //skip "Bearer " prefix
            // Retrieve the JWK with a matching key ID (kid)
            var jwk = jwkSet.getKeyByKeyId(signedJWT.getHeader().getKeyID());
            if (jwk == null)  //internal problem...
                return HttpStatus.INTERNAL_SERVER_ERROR;
            // Verify the JWT signature using the public key
            var verifier = new RSASSAVerifier(jwk.toRSAKey());
            if (!signedJWT.verify(verifier))
                return HttpStatus.FORBIDDEN; //cannot verify, let's return FORBIDDEN
            // Check the 'aud' claim
            var audValues = signedJWT.getJWTClaimsSet().getAudience();
            if (!audValues.contains(audienceToCheck))
                return HttpStatus.FORBIDDEN;
        } catch (ParseException | JOSEException e) {
            return HttpStatus.FORBIDDEN;
        }
        return HttpStatus.OK;
    }

    @Override
    public synchronized String getManagementApiToken() {
        return managementApiToken;
    }

    @Override
    public synchronized void setManagementApiToken(String value) {
        this.managementApiToken = value;
    }

    @Override
    public synchronized void setJwkSet(JWKSet jwkSet) {
        this.jwkSet = jwkSet;
    }
}
