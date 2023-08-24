package gr.jimmys.jimmysfoodzilla.controllers;

import com.google.gson.JsonArray;
import gr.jimmys.jimmysfoodzilla.DTO.Auth0UserSerialize;
import gr.jimmys.jimmysfoodzilla.DTO.UserMetadata;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.models.User;
import gr.jimmys.jimmysfoodzilla.repository.UserRepository;
import gr.jimmys.jimmysfoodzilla.services.JwtValidationAndRenewalService;
import gr.jimmys.jimmysfoodzilla.utils.Tuple3;
import kong.unirest.*;
import kong.unirest.json.JSONArray;
import kong.unirest.json.JSONException;
import kong.unirest.json.JSONObject;
import org.apache.coyote.Response;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.repository.query.Param;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.io.UnsupportedEncodingException;
import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.time.LocalDateTime;
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
            HttpResponse<Empty> response = Unirest.patch("https://" + auth0_domain + "/api/v2/users/" + URLEncoder.encode(user.getUser_Id(), StandardCharsets.UTF_8))
                    .header("Authorization", "Bearer " + mApiToken)
                    .header("Content-type", "application/json")
                    .header("Accept", "application/json")
                    .body(auth0UserToSend)
                    .asEmpty();
            if (response.getStatus() != 200) {
                logger.error("UserController: Error in ManagementAPI api/v2/users/" + user.getUser_Id() + " HTTP PATCH request, could not patch user\n" +
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
