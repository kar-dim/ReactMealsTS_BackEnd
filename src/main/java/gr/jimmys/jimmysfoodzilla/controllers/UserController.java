package gr.jimmys.jimmysfoodzilla.controllers;

import com.nimbusds.jose.JOSEException;
import com.nimbusds.jose.JWSVerifier;
import com.nimbusds.jose.crypto.RSASSAVerifier;
import com.nimbusds.jose.jwk.JWK;
import com.nimbusds.jose.jwk.JWKSet;
import com.nimbusds.jwt.SignedJWT;
import gr.jimmys.jimmysfoodzilla.DTO.Auth0UserSerialize;
import gr.jimmys.jimmysfoodzilla.DTO.UserMetadata;
import gr.jimmys.jimmysfoodzilla.models.User;
import gr.jimmys.jimmysfoodzilla.repository.UserRepository;
import gr.jimmys.jimmysfoodzilla.services.JwtValidationAndRenewalService;
import kong.unirest.*;
import kong.unirest.json.JSONArray;
import kong.unirest.json.JSONException;
import kong.unirest.json.JSONObject;
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

@RestController
@RequestMapping("/api/Users")
public class UserController {

    @Autowired
    private JwtValidationAndRenewalService jwtValidationAndRenewalService;

    private final Logger logger = LoggerFactory.getLogger(UserController.class);

    @Value("${auth0.domain}")
    private String auth0_domain;

    @Value("${auth0.m2maudience}")
    private String auth0_m2maudience;
    @Autowired
    UserRepository userRepository;

    @GetMapping("/GetUsers")
    public ResponseEntity<List<User>> getUsers() {
        String mApiToken = jwtValidationAndRenewalService.getManagementApiAccessTokenValue();
        //check if management api token exists from the injected service
        if (mApiToken == null || mApiToken.trim().isEmpty()) {
            logger.error("UserController: ManagementAPI Token does not exist");
            return ResponseEntity.internalServerError().build();
        }

        try {
            HttpResponse<JsonNode> response = Unirest.get("https://" + auth0_domain + "/api/v2/users")
                    .header("Authorization", "Bearer " + mApiToken)
                    .asJson();
            if (response.getStatus() != 200) {
                logger.error("UserController: Error in ManagementAPI api/v2/users HTTP GET request, could not get users\n" +
                        "Reason: STATUS CODE: " + response.getStatus() + " STATUS TEXT: " + response.getStatusText());
                return ResponseEntity.internalServerError().build();
            }
            // Parse the JSON response
            JsonNode jsonNode = response.getBody();
            if (jsonNode == null || jsonNode.getArray().length() == 0) {
                logger.error("UserController: Users returned are malformed! Check Auth0 configuration");
                return ResponseEntity.internalServerError().build();
            }
            JSONArray usersArray = jsonNode.getArray();
            List<User> usersToReturn = new ArrayList<>();
            for (int i=0; i<usersArray.length(); i++) {
                JSONObject userElements = usersArray.getJSONObject(i);
                try {
                    String userId = userElements.getString("user_id");
                    String email = userElements.getString("email");
                    JSONObject userMetadata = userElements.getJSONObject("user_metadata");
                    String name = userMetadata.getString("name");
                    String lastName = userMetadata.getString("last_name");
                    String address = userMetadata.getString("address");

                    usersToReturn.add(new User(userId, email, name, lastName, address));
                } catch (JSONException je) {
                    //skip this user if there are invalid/missing data
                }
            }
            return new ResponseEntity<>(usersToReturn, HttpStatus.OK);
        } catch (UnirestException e) {
            logger.error("UserController: Error in ManagementAPI api/v2/users HTTP GET request");
            return ResponseEntity.internalServerError().build();
        }

    }

    @PostMapping("/CreateUser")
    public ResponseEntity<User> createUser(@RequestHeader(HttpHeaders.AUTHORIZATION) String token, @RequestBody User userToCreate) {
        if (token == null || !token.startsWith("Bearer ")) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        }
        try {
            // Extract the JWT token
            SignedJWT signedJWT = SignedJWT.parse(token.substring(7)); //skip "Bearer " prefix
            // Fetch the JWKS URL (cached)
            JWKSet jwkSet = jwtValidationAndRenewalService.getJwkSet();
            if (jwkSet == null) //internal problem...
                return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).build();
            // Retrieve the JWK with a matching key ID (kid)
            JWK jwk = jwkSet.getKeyByKeyId(signedJWT.getHeader().getKeyID());
            if (jwk == null)  //internal problem...
                return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).build();
            // Verify the JWT signature using the public key
            JWSVerifier verifier = new RSASSAVerifier(jwk.toRSAKey());
            if (!signedJWT.verify(verifier))
                return ResponseEntity.status(HttpStatus.FORBIDDEN).build(); //cannot verify, let's return FORBIDDEN
            // Check the 'aud' claim
            List<String> audience = signedJWT.getJWTClaimsSet().getAudience();
            if (audience == null || !audience.contains(auth0_m2maudience))
                return ResponseEntity.status(HttpStatus.FORBIDDEN).build();

        } catch (ParseException | JOSEException e) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        }
        //JWT checks passed, insert the user to DB
        logger.info("UserController: New User Created [Sent from Auth0]: " + userToCreate.toString());
        boolean exists = userRepository.existsById(userToCreate.getUser_id());
        if (exists)
        {
            logger.error("UserController: Error: User already exists");
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, "User Already Exists!");
        }
        //else, add the user
        userRepository.save(userToCreate);
        return new ResponseEntity<>(userToCreate, HttpStatus.OK);
    }

    @PutMapping("/UpdateUser")
    public ResponseEntity<Void> updateUser(@RequestBody User user) {
        String mApiToken = jwtValidationAndRenewalService.getManagementApiAccessTokenValue();
        //check if management api token exists from the injected service
        if (mApiToken == null || mApiToken.trim().isEmpty()) {
            logger.error("UserController: ManagementAPI Token does not exist");
            return ResponseEntity.internalServerError().build();
        }

        try {
            Auth0UserSerialize auth0UserToSend = new Auth0UserSerialize(user.getEmail(), new UserMetadata(user.getName(), user.getLastName(), user.getAddress()));
            HttpResponse<Empty> response = Unirest.patch("https://" + auth0_domain + "/api/v2/users/" + URLEncoder.encode(user.getUser_id(), StandardCharsets.UTF_8))
                    .header("Authorization", "Bearer " + mApiToken)
                    .header("Content-type", "application/json")
                    .header("Accept", "application/json")
                    .body(auth0UserToSend)
                    .asEmpty();
            if (response.getStatus() != 200) {
                logger.error("UserController: Error in ManagementAPI api/v2/users/" + user.getUser_id() + " HTTP PATCH request, could not patch user\n" +
                        "Reason: STATUS CODE: " + response.getStatus() + " STATUS TEXT: " + response.getStatusText());
                return ResponseEntity.internalServerError().build();
            }
            //we can delete the user's orders from our own db (Order, OrderItem tables)
            //but let's keep them for "archival/proof" reasons
            return new ResponseEntity<>(HttpStatus.OK);
        } catch (UnirestException e) {
            logger.error("UserController: Error in ManagementAPI api/v2/users HTTP PATCH request");
            return ResponseEntity.internalServerError().build();
        }

    }

    @DeleteMapping("/DeleteUser/{userId}")
    public ResponseEntity<Void> deleteUser(@PathVariable("userId") String userId) {
        String mApiToken = jwtValidationAndRenewalService.getManagementApiAccessTokenValue();
        //check if management api token exists from the injected service
        if (mApiToken == null || mApiToken.trim().isEmpty()) {
            logger.error("UserController: ManagementAPI Token does not exist");
            return ResponseEntity.internalServerError().build();
        }

        try {
            HttpResponse<Empty> response = Unirest.delete("https://" + auth0_domain + "/api/v2/users/" + URLEncoder.encode(userId, StandardCharsets.UTF_8))
                    .header("Authorization", "Bearer " + mApiToken)
                    .asEmpty();
            //DELETE ok status is 204
            if (response.getStatus() != 204) {
                logger.error("UserController: Error in ManagementAPI api/v2/users/" + userId + " HTTP DELETE request, could not delete user\n" +
                        "Reason: STATUS CODE: " + response.getStatus() + " STATUS TEXT: " + response.getStatusText());
                return ResponseEntity.internalServerError().build();
            }
            //we can delete the user's orders from our own db (Order, OrderItem tables)
            //but let's keep them for "archival/proof" reasons
            return new ResponseEntity<>(HttpStatus.OK);
        } catch (UnirestException e) {
            logger.error("UserController: Error in ManagementAPI api/v2/users HTTP DELETE request");
            return ResponseEntity.internalServerError().build();
        }

    }

}
