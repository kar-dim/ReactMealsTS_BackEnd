package gr.jimmys.jimmysfoodzilla.services.impl;

import tools.jackson.core.type.TypeReference;
import tools.jackson.databind.ObjectMapper;
import gr.jimmys.jimmysfoodzilla.dto.Auth0UserDeserialize;
import gr.jimmys.jimmysfoodzilla.dto.Auth0UserSerialize;
import gr.jimmys.jimmysfoodzilla.exception.Auth0ManagementException;
import gr.jimmys.jimmysfoodzilla.services.api.JwtRenewalService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.net.URI;
import java.net.URLEncoder;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.List;

/**
 * Simple client over the Auth0 Management API. Used for M2M-token lookup, request building, timeout and error handling
 * any failure (missing token, network error, non success status etc) is thrown as {@link Auth0ManagementException}
 */
@Component
public class Auth0ManagementClient {
    private final Logger logger = LoggerFactory.getLogger(Auth0ManagementClient.class);

    private static final Duration REQUEST_TIMEOUT = Duration.ofSeconds(30);

    private final JwtRenewalService jwtRenewalService;
    private final HttpClient httpClient;
    private final ObjectMapper objectMapper;
    private final String auth0Domain;

    public Auth0ManagementClient(JwtRenewalService jwtRenewalService,
                                 HttpClient httpClient,
                                 ObjectMapper objectMapper,
                                 @Value("${auth0.domain}") String auth0Domain) {
        this.jwtRenewalService = jwtRenewalService;
        this.httpClient = httpClient;
        this.objectMapper = objectMapper;
        this.auth0Domain = auth0Domain;
    }

    public List<Auth0UserDeserialize> getUsers() {
        HttpResponse<String> response = send(
                authorizedRequest("/api/v2/users").GET().build(),
                HttpResponse.BodyHandlers.ofString(), "GET /api/v2/users");
        if (response.statusCode() != 200)
            throw failedStatus("GET /api/v2/users", response.statusCode());
        return objectMapper.readValue(response.body(), new TypeReference<>() {});
    }

    public void updateUser(String userId, Auth0UserSerialize body) {
        String json = objectMapper.writeValueAsString(body);
        String op = "PATCH /api/v2/users/" + userId;
        HttpResponse<Void> response = send(
                authorizedRequest("/api/v2/users/" + encode(userId))
                        .header("Content-Type", "application/json")
                        .header("Accept", "application/json")
                        .method("PATCH", HttpRequest.BodyPublishers.ofString(json))
                        .build(),
                HttpResponse.BodyHandlers.discarding(), op);
        if (response.statusCode() != 200)
            throw failedStatus(op, response.statusCode());
    }

    public void deleteUser(String userId) {
        String op = "DELETE /api/v2/users/" + userId;
        HttpResponse<Void> response = send(
                authorizedRequest("/api/v2/users/" + encode(userId)).DELETE().build(),
                HttpResponse.BodyHandlers.discarding(), op);
        if (response.statusCode() != 204)
            throw failedStatus(op, response.statusCode());
    }

    private HttpRequest.Builder authorizedRequest(String path) {
        return HttpRequest.newBuilder()
                .uri(URI.create("https://" + auth0Domain + path))
                .header("Authorization", "Bearer " + managementToken())
                .timeout(REQUEST_TIMEOUT);
    }

    private <T> HttpResponse<T> send(HttpRequest request, HttpResponse.BodyHandler<T> handler, String op) {
        try {
            return httpClient.send(request, handler);
        } catch (IOException e) {
            logger.error("ManagementAPI {} failed: {}", op, e.getMessage());
            throw new Auth0ManagementException("ManagementAPI " + op + " failed");
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            throw new Auth0ManagementException("ManagementAPI " + op + " interrupted");
        }
    }

    private String managementToken() {
        var token = jwtRenewalService.getManagementApiToken();
        if (token == null || token.trim().isEmpty()) {
            logger.error("ManagementAPI Token does not exist");
            throw new Auth0ManagementException("ManagementAPI token unavailable");
        }
        return token;
    }

    private Auth0ManagementException failedStatus(String op, int status) {
        logger.error("ManagementAPI {} failed: status {}", op, status);
        return new Auth0ManagementException("ManagementAPI " + op + " returned status " + status);
    }

    private static String encode(String value) {
        return URLEncoder.encode(value, StandardCharsets.UTF_8);
    }
}
