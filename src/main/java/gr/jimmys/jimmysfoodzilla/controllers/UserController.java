package gr.jimmys.jimmysfoodzilla.controllers;

import com.nimbusds.jose.JOSEException;
import com.nimbusds.jose.JWSVerifier;
import com.nimbusds.jose.crypto.RSASSAVerifier;
import com.nimbusds.jose.jwk.JWK;
import com.nimbusds.jose.jwk.JWKSet;
import com.nimbusds.jwt.SignedJWT;
import gr.jimmys.jimmysfoodzilla.dto.Auth0UserDeserialize;
import gr.jimmys.jimmysfoodzilla.dto.Auth0UserSerialize;
import gr.jimmys.jimmysfoodzilla.dto.UserMetadata;
import gr.jimmys.jimmysfoodzilla.models.User;
import gr.jimmys.jimmysfoodzilla.repository.UserRepository;
import gr.jimmys.jimmysfoodzilla.services.api.JwtRenewalService;
import kong.unirest.core.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;

import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.text.ParseException;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/api/Users")
public class UserController {
    private final Logger logger = LoggerFactory.getLogger(UserController.class);

    @Autowired
    UserRepository userRepository;

    @Autowired
    private JwtRenewalService jwtRenewalService;

    @Value("${auth0.domain}")
    private String auth0_domain;

    @Value("${auth0.m2maudience}")
    private String audience;

    @GetMapping("/GetUsers")
    public ResponseEntity<List<User>> getUsers() {
        String mApiToken = jwtRenewalService.getManagementApiToken();
        //check if management api token exists from the injected service
        if (mApiToken == null || mApiToken.trim().isEmpty()) {
            logger.error("ManagementAPI Token does not exist");
            return ResponseEntity.internalServerError().build();
        }
        try {
            HttpResponse<List<Auth0UserDeserialize>> response = Unirest.get("https://" + auth0_domain + "/api/v2/users")
                    .header("Authorization", "Bearer " + mApiToken)
                    .asObject(new GenericType<>() {});
            if (response.getStatus() != 200 || response.getBody() == null) {
                logger.error("Error in ManagementAPI api/v2/users HTTP GET request, could not get users\nReason: STATUS CODE: {} STATUS TEXT: {}", response.getStatus(), response.getStatusText());
                return ResponseEntity.internalServerError().build();
            }
            // Parse the JSON response
            var users = response.getBody();
            if (users.isEmpty()) {
                logger.error("Users returned are malformed! Check Auth0 configuration");
                return ResponseEntity.internalServerError().build();
            }
            //filter and return valid users
            var usersToReturn = users.stream()
                    .filter(Objects::nonNull)
                    .filter(Auth0UserDeserialize::isValidUser)
                    .filter(user -> !"admin".equals(user.getNickname()))
                    .map(user -> new User(user.getUserId(), user.getEmail(), user.getUserMetadata().getName(), user.getUserMetadata().getLastName(), user.getUserMetadata().getAddress()))
                    .collect(Collectors.toList());
            return new ResponseEntity<>(usersToReturn, HttpStatus.OK);
        } catch (UnirestException e) {
            logger.error("Error in ManagementAPI api/v2/users HTTP GET request");
            return ResponseEntity.internalServerError().build();
        }
    }

    @PostMapping("/CreateUser")
    public ResponseEntity<User> createUser(@RequestHeader(HttpHeaders.AUTHORIZATION) String token, @RequestBody User userToCreate) {
        if (token == null || !token.startsWith("Bearer ")) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        }
        var tokenValidationStatus = jwtRenewalService.validateToken(token);
        if (tokenValidationStatus != HttpStatus.OK)
            return ResponseEntity.status(tokenValidationStatus).build();
        //JWT checks passed, insert the user to DB if not already exists
        if (userRepository.existsById(userToCreate.getUserId()))
        {
            logger.error("Error: User already exists");
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, "User Already Exists!");
        }
        userRepository.save(userToCreate);
        logger.info("New User Created [Sent from Auth0]: {}", userToCreate);
        return new ResponseEntity<>(userToCreate, HttpStatus.OK);
    }

    @PutMapping("/UpdateUser")
    public ResponseEntity<Void> updateUser(@RequestBody User user) {
        String mApiToken = jwtRenewalService.getManagementApiToken();
        //check if management api token exists from the injected service
        if (mApiToken == null || mApiToken.trim().isEmpty()) {
            logger.error("ManagementAPI Token does not exist");
            return ResponseEntity.internalServerError().build();
        }
        try {
            var userToSend = new Auth0UserSerialize(user.getEmail(), new UserMetadata(user.getName(), user.getLastName(), user.getAddress()));
            HttpResponse<Empty> response = Unirest.patch("https://" + auth0_domain + "/api/v2/users/" + URLEncoder.encode(user.getUserId(), StandardCharsets.UTF_8))
                    .header("Authorization", "Bearer " + mApiToken)
                    .header("Content-type", "application/json")
                    .header("Accept", "application/json")
                    .body(userToSend)
                    .asEmpty();
            if (response.getStatus() != 200) {
                logger.error("Error in ManagementAPI api/v2/users/{} HTTP PATCH request, could not patch user\nReason: STATUS CODE: {} STATUS TEXT: {}", user.getUserId(), response.getStatus(), response.getStatusText());
                return ResponseEntity.internalServerError().build();
            }
            //we can delete the user's orders from our own db (Order, OrderItem tables)
            //but let's keep them for "archival/proof" reasons
            return new ResponseEntity<>(HttpStatus.OK);
        } catch (UnirestException e) {
            logger.error("Error in ManagementAPI api/v2/users HTTP PATCH request");
            return ResponseEntity.internalServerError().build();
        }
    }

    @DeleteMapping("/DeleteUser/{userId}")
    public ResponseEntity<Void> deleteUser(@PathVariable("userId") String userId) {
        String mApiToken = jwtRenewalService.getManagementApiToken();
        //check if management api token exists from the injected service
        if (mApiToken == null || mApiToken.trim().isEmpty()) {
            logger.error("ManagementAPI Token does not exist");
            return ResponseEntity.internalServerError().build();
        }
        try {
            HttpResponse<Empty> response = Unirest.delete("https://" + auth0_domain + "/api/v2/users/" + URLEncoder.encode(userId, StandardCharsets.UTF_8))
                    .header("Authorization", "Bearer " + mApiToken)
                    .asEmpty();
            //DELETE ok status is 204
            if (response.getStatus() != 204) {
                logger.error("Error in ManagementAPI api/v2/users/" + userId + " HTTP DELETE request, could not delete user\n" +
                        "Reason: STATUS CODE: " + response.getStatus() + " STATUS TEXT: " + response.getStatusText());
                return ResponseEntity.internalServerError().build();
            }
            //we can delete the user's orders from our own db (Order, OrderItem tables)
            //but let's keep them for "archival/proof" reasons
            return new ResponseEntity<>(HttpStatus.OK);
        } catch (UnirestException e) {
            logger.error("Error in ManagementAPI api/v2/users HTTP DELETE request");
            return ResponseEntity.internalServerError().build();
        }
    }
}
