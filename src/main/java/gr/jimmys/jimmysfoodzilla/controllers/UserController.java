package gr.jimmys.jimmysfoodzilla.controllers;

import tools.jackson.core.type.TypeReference;
import tools.jackson.databind.ObjectMapper;
import gr.jimmys.jimmysfoodzilla.dto.Auth0UserDeserialize;
import gr.jimmys.jimmysfoodzilla.dto.Auth0UserSerialize;
import gr.jimmys.jimmysfoodzilla.dto.UserMetadata;
import gr.jimmys.jimmysfoodzilla.models.User;
import gr.jimmys.jimmysfoodzilla.repository.UserRepository;
import gr.jimmys.jimmysfoodzilla.services.api.JwtRenewalService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtException;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;

import java.io.IOException;
import java.net.URI;
import java.net.URLEncoder;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.List;
import java.util.Objects;

@RestController
@RequestMapping("/api/Users")
public class UserController {
    private final Logger logger = LoggerFactory.getLogger(UserController.class);

    private static final Duration REQUEST_TIMEOUT = Duration.ofSeconds(30);

    @Autowired
    UserRepository userRepository;

    @Autowired
    private JwtRenewalService jwtRenewalService;

    @Autowired
    @Qualifier("m2mJwtDecoder")
    private JwtDecoder m2mJwtDecoder;

    @Autowired
    private HttpClient httpClient;

    @Autowired
    private ObjectMapper objectMapper;

    @Value("${auth0.domain}")
    private String auth0_domain;

    @GetMapping("/GetUsers")
    public ResponseEntity<List<User>> getUsers() {
        var mApiToken = getManagementApiToken();
        if (mApiToken.isEmpty())
            return ResponseEntity.internalServerError().build();
        try {
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create("https://" + auth0_domain + "/api/v2/users"))
                    .header("Authorization", "Bearer " + mApiToken)
                    .GET()
                    .timeout(REQUEST_TIMEOUT)
                    .build();
            HttpResponse<String> response = httpClient.send(request, HttpResponse.BodyHandlers.ofString());
            if (response.statusCode() != 200) {
                logger.error("ManagementAPI GET /api/v2/users failed: status {}", response.statusCode());
                return ResponseEntity.internalServerError().build();
            }
            List<Auth0UserDeserialize> users = objectMapper.readValue(response.body(), new TypeReference<>() {});
            if (users.isEmpty()) {
                logger.error("Users returned are malformed! Check Auth0 configuration");
                return ResponseEntity.internalServerError().build();
            }
            var usersToReturn = users.stream()
                    .filter(Objects::nonNull)
                    .filter(Auth0UserDeserialize::isValidUser)
                    .map(user -> new User(user.getUserId(), user.getEmail(),
                            user.getUserMetadata().getName(),
                            user.getUserMetadata().getLastName(),
                            user.getUserMetadata().getAddress()))
                    .toList();
            return new ResponseEntity<>(usersToReturn, HttpStatus.OK);
        } catch (IOException e) {
            logger.error("ManagementAPI GET /api/v2/users failed: {}", e.getMessage());
            return ResponseEntity.internalServerError().build();
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            return ResponseEntity.internalServerError().build();
        }
    }

    @PostMapping("/CreateUser")
    public ResponseEntity<User> createUser(@RequestHeader(HttpHeaders.AUTHORIZATION) String authHeader, @RequestBody User userToCreate) {
        if (authHeader == null || !authHeader.startsWith("Bearer "))
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        try {
            m2mJwtDecoder.decode(authHeader.substring(7));
        } catch (JwtException e) {
            logger.warn("CreateUser: M2M token validation failed - {}", e.getMessage());
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        }
        if (userRepository.existsById(userToCreate.getUserId())) {
            logger.error("Error: User already exists");
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, "User Already Exists!");
        }
        userRepository.save(userToCreate);
        logger.info("New User Created [Sent from Auth0]: {}", userToCreate);
        return new ResponseEntity<>(userToCreate, HttpStatus.OK);
    }

    @PutMapping("/UpdateUser")
    public ResponseEntity<Void> updateUser(@RequestBody User newUser) {
        var mApiToken = getManagementApiToken();
        if (mApiToken.isEmpty())
            return ResponseEntity.internalServerError().build();
        try {
            var userToSend = new Auth0UserSerialize(newUser.getEmail(),
                    new UserMetadata(newUser.getName(), newUser.getLastName(), newUser.getAddress()));
            String body = objectMapper.writeValueAsString(userToSend);
            String encodedId = URLEncoder.encode(newUser.getUserId(), StandardCharsets.UTF_8);
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create("https://" + auth0_domain + "/api/v2/users/" + encodedId))
                    .header("Authorization", "Bearer " + mApiToken)
                    .header("Content-Type", "application/json")
                    .header("Accept", "application/json")
                    .method("PATCH", HttpRequest.BodyPublishers.ofString(body))
                    .timeout(REQUEST_TIMEOUT)
                    .build();
            HttpResponse<Void> response = httpClient.send(request, HttpResponse.BodyHandlers.discarding());
            if (response.statusCode() != 200) {
                logger.error("ManagementAPI PATCH /api/v2/users/{} failed: status {}", newUser.getUserId(), response.statusCode());
                return ResponseEntity.internalServerError().build();
            }
            userRepository.save(newUser);
            return new ResponseEntity<>(HttpStatus.OK);
        } catch (IOException e) {
            logger.error("ManagementAPI PATCH /api/v2/users failed: {}", e.getMessage());
            return ResponseEntity.internalServerError().build();
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            return ResponseEntity.internalServerError().build();
        }
    }

    @DeleteMapping("/DeleteUser/{userId}")
    public ResponseEntity<Void> deleteUser(@PathVariable("userId") String userId) {
        var mApiToken = getManagementApiToken();
        if (mApiToken.isEmpty())
            return ResponseEntity.internalServerError().build();
        try {
            String encodedId = URLEncoder.encode(userId, StandardCharsets.UTF_8);
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create("https://" + auth0_domain + "/api/v2/users/" + encodedId))
                    .header("Authorization", "Bearer " + mApiToken)
                    .DELETE()
                    .timeout(REQUEST_TIMEOUT)
                    .build();
            HttpResponse<Void> response = httpClient.send(request, HttpResponse.BodyHandlers.discarding());
            if (response.statusCode() != 204) {
                logger.error("ManagementAPI DELETE /api/v2/users/{} failed: status {}", userId, response.statusCode());
                return ResponseEntity.internalServerError().build();
            }
            return new ResponseEntity<>(HttpStatus.OK);
        } catch (IOException e) {
            logger.error("ManagementAPI DELETE /api/v2/users failed: {}", e.getMessage());
            return ResponseEntity.internalServerError().build();
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            return ResponseEntity.internalServerError().build();
        }
    }

    private String getManagementApiToken() {
        var token = jwtRenewalService.getManagementApiToken();
        if (token == null || token.trim().isEmpty()) {
            logger.error("ManagementAPI Token does not exist");
            return "";
        }
        return token;
    }
}
