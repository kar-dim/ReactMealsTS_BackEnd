package gr.jimmys.jimmysfoodzilla.controllers;

import gr.jimmys.jimmysfoodzilla.dto.Auth0UserDeserialize;
import gr.jimmys.jimmysfoodzilla.dto.Auth0UserSerialize;
import gr.jimmys.jimmysfoodzilla.dto.UserMetadata;
import gr.jimmys.jimmysfoodzilla.exception.Auth0ManagementException;
import gr.jimmys.jimmysfoodzilla.models.User;
import gr.jimmys.jimmysfoodzilla.repository.UserRepository;
import gr.jimmys.jimmysfoodzilla.services.impl.Auth0ManagementClient;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtException;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;

import java.util.List;
import java.util.Objects;

@RestController
@RequestMapping("/api/Users")
public class UserController {
    private final Logger logger = LoggerFactory.getLogger(UserController.class);

    private final UserRepository userRepository;
    private final JwtDecoder m2mJwtDecoder;
    private final Auth0ManagementClient managementClient;

    public UserController(UserRepository userRepository,
                          @Qualifier("m2mJwtDecoder") JwtDecoder m2mJwtDecoder,
                          Auth0ManagementClient managementClient) {
        this.userRepository = userRepository;
        this.m2mJwtDecoder = m2mJwtDecoder;
        this.managementClient = managementClient;
    }

    @GetMapping("/GetUsers")
    public ResponseEntity<List<User>> getUsers() {
        try {
            List<Auth0UserDeserialize> users = managementClient.getUsers();
            if (users.isEmpty()) {
                logger.error("Users returned are malformed! Check Auth0 configuration");
                throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, gr.jimmys.jimmysfoodzilla.common.ErrorMessages.INTERNAL_ERROR);
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
        } catch (Auth0ManagementException e) {
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, gr.jimmys.jimmysfoodzilla.common.ErrorMessages.INTERNAL_ERROR);
        }
    }

    @PostMapping("/CreateUser")
    public ResponseEntity<User> createUser(@RequestHeader(value = HttpHeaders.AUTHORIZATION, required = false) String authHeader,
                                           @RequestBody User userToCreate) {
        if (authHeader == null || !authHeader.startsWith("Bearer "))
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        try {
            m2mJwtDecoder.decode(authHeader.substring(7));
        } catch (JwtException e) {
            logger.warn("CreateUser: M2M token validation failed - {}", e.getMessage());
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        }
        if (userToCreate == null || userToCreate.getUserId() == null || userToCreate.getUserId().isBlank())
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Missing user id");
        if (userRepository.existsById(userToCreate.getUserId())) {
            logger.error("CreateUser: user {} already exists", userToCreate.getUserId());
            throw new ResponseStatusException(HttpStatus.CONFLICT, "User Already Exists!");
        }
        userRepository.save(userToCreate);
        logger.info("New User Created [Sent from Auth0]: {}", userToCreate);
        return ResponseEntity.ok(userToCreate);
    }

    @PutMapping("/UpdateUser")
    public ResponseEntity<Void> updateUser(@RequestBody User newUser) {
        if (newUser == null || newUser.getUserId() == null || newUser.getUserId().isBlank())
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Missing user id");
        if (!userRepository.existsById(newUser.getUserId()))
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "User not found");

        try {
            var userToSend = new Auth0UserSerialize(newUser.getEmail(),
                    new UserMetadata(newUser.getName(), newUser.getLastName(), newUser.getAddress()));
            managementClient.updateUser(newUser.getUserId(), userToSend);
            userRepository.save(newUser);
            return ResponseEntity.ok().build();
        } catch (Auth0ManagementException e) {
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, gr.jimmys.jimmysfoodzilla.common.ErrorMessages.INTERNAL_ERROR);
        }
    }

    @DeleteMapping("/DeleteUser/{userId}")
    public ResponseEntity<Void> deleteUser(@PathVariable("userId") String userId) {
        if (userId == null || userId.isBlank())
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Missing user id");
        try {
            managementClient.deleteUser(userId);
            return ResponseEntity.ok().build();
        } catch (Auth0ManagementException e) {
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, gr.jimmys.jimmysfoodzilla.common.ErrorMessages.INTERNAL_ERROR);
        }
    }
}
