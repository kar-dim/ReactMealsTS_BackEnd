package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.dto.ManagementInputDTO;
import gr.jimmys.jimmysfoodzilla.dto.ManagementResponseDTO;
import gr.jimmys.jimmysfoodzilla.models.Token;
import gr.jimmys.jimmysfoodzilla.repository.TokenRepository;
import gr.jimmys.jimmysfoodzilla.services.api.JwtService;
import jakarta.annotation.PostConstruct;
import kong.unirest.core.HttpResponse;
import kong.unirest.core.Unirest;
import kong.unirest.core.UnirestException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;

@Service
public class JwtServiceImpl implements JwtService {
    private final Logger logger = LoggerFactory.getLogger(JwtServiceImpl.class);

    @Autowired
    private TokenRepository tokenRepository;

    @Value("${auth0.domain}")
    private String domain;

    @Value("${auth0.m2m_clientid}")
    private String clientId;

    @Value("${auth0.m2m_clientsecret}")
    private String clientSecret;

    private ManagementInputDTO requestBody;

    @PostConstruct
    public void init() {
        requestBody = new ManagementInputDTO(clientId, clientSecret, "https://" + domain + "/api/v2/", "client_credentials");
    }

    @Override
    public Token retrieveToken() {
        var tokenFromDb = tokenRepository.getManagementApiToken();
        return tokenFromDb.orElseGet(() -> {
            logger.info("No ManagementAPI Token found in db...");
            return null;
        });
    }

    @Override
    public Token renewToken() {
        try {
            HttpResponse<ManagementResponseDTO> response = Unirest.post("https://" + domain + "/oauth/token")
                    .header("content-type", "application/json")
                    .body(requestBody)
                    .asObject(ManagementResponseDTO.class);
            if (response.getStatus() != 200) {
                logger.error("Error in ManagementAPI oauth/token HTTP POST request, could not receive token\nReason: STATUS CODE: {} STATUS TEXT: {}", response.getStatus(), response.getStatusText());
                return null;
            }
            // Extract data
            var tokenData = response.getBody();
            if (tokenData.getExpiresIn() == 0 || tokenData.getAccessToken() == null || tokenData.getTokenType() == null) {
                logger.error("ManagementAPI token is malformed! Check Auth0 configuration");
                return null;
            }

            //delete old token from db
            tokenRepository.removeManagementApiToken();
            //create the new Token entity
            Token newToken = new Token(0, tokenData.getAccessToken(), Token.MANAGEMENT_API, LocalDateTime.now().plusSeconds(tokenData.getExpiresIn()));
            //save to db
            tokenRepository.save(newToken);
            logger.info("Auth0 Management API Token successfully saved");
            return newToken;

        } catch (UnirestException e) {
            logger.error("Error in ManagementAPI oauth/token HTTP POST request, could not receive token");
            return null;
        }
    }
}
