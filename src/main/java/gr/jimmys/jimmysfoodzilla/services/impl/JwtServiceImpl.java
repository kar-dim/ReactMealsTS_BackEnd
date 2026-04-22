package gr.jimmys.jimmysfoodzilla.services.impl;

import tools.jackson.databind.ObjectMapper;
import gr.jimmys.jimmysfoodzilla.dto.ManagementInputDTO;
import gr.jimmys.jimmysfoodzilla.dto.ManagementResponseDTO;
import gr.jimmys.jimmysfoodzilla.models.Token;
import gr.jimmys.jimmysfoodzilla.repository.TokenRepository;
import gr.jimmys.jimmysfoodzilla.services.api.JwtService;
import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.time.Duration;
import java.time.LocalDateTime;

@Service
public class JwtServiceImpl implements JwtService {
    private final Logger logger = LoggerFactory.getLogger(JwtServiceImpl.class);

    private static final Duration REQUEST_TIMEOUT = Duration.ofSeconds(30);

    @Autowired
    private TokenRepository tokenRepository;

    @Autowired
    private HttpClient httpClient;

    @Autowired
    private ObjectMapper objectMapper;

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
        return tokenRepository.getManagementApiToken().orElseGet(() -> {
            logger.info("No ManagementAPI Token found in db...");
            return null;
        });
    }

    @Override
    public Token renewToken() throws InterruptedException {
        try {
            String body = objectMapper.writeValueAsString(requestBody);
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create("https://" + domain + "/oauth/token"))
                    .header("Content-Type", "application/json")
                    .POST(HttpRequest.BodyPublishers.ofString(body))
                    .timeout(REQUEST_TIMEOUT)
                    .build();

            HttpResponse<String> response = httpClient.send(request, HttpResponse.BodyHandlers.ofString());
            if (response.statusCode() != 200) {
                logger.error("ManagementAPI oauth/token POST failed: status {}", response.statusCode());
                return null;
            }
            var tokenData = objectMapper.readValue(response.body(), ManagementResponseDTO.class);
            if (tokenData.getExpiresIn() == 0 || tokenData.getAccessToken() == null || tokenData.getTokenType() == null) {
                logger.error("ManagementAPI token is malformed! Check Auth0 configuration");
                return null;
            }
            tokenRepository.removeManagementApiToken();
            Token newToken = new Token(0, tokenData.getAccessToken(), Token.MANAGEMENT_API,
                    LocalDateTime.now().plusSeconds(tokenData.getExpiresIn()));
            tokenRepository.save(newToken);
            logger.info("Auth0 Management API Token successfully saved");
            return newToken;
        } catch (IOException e) {
            logger.error("ManagementAPI oauth/token POST failed: {}", e.getMessage());
            return null;
        }
    }
}
