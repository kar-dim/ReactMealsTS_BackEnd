package gr.jimmys.jimmysfoodzilla.controllers;

import com.nimbusds.jose.JOSEException;
import com.nimbusds.jose.JWSVerifier;
import com.nimbusds.jose.crypto.RSASSAVerifier;
import com.nimbusds.jose.jwk.JWK;
import com.nimbusds.jose.jwk.JWKSet;
import com.nimbusds.jwt.SignedJWT;
import gr.jimmys.jimmysfoodzilla.dto.*;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.models.Order;
import gr.jimmys.jimmysfoodzilla.models.User;
import gr.jimmys.jimmysfoodzilla.repository.DishRepository;
import gr.jimmys.jimmysfoodzilla.repository.OrderRepository;
import gr.jimmys.jimmysfoodzilla.repository.UserRepository;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import gr.jimmys.jimmysfoodzilla.services.api.DishImageService;
import gr.jimmys.jimmysfoodzilla.services.api.JwtRenewalService;
import gr.jimmys.jimmysfoodzilla.utils.WebOrderDTOToEntity;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.core.io.ResourceLoader;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.MathContext;
import java.math.RoundingMode;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.text.ParseException;
import java.util.ArrayList;
import java.util.Base64;
import java.util.List;
import java.util.Optional;

@RestController
@RequestMapping("/api/Dishes")
public class DishController {
    private final Logger logger = LoggerFactory.getLogger(DishController.class);

    @Autowired
    DishRepository dishRepository;

    @Autowired
    OrderRepository orderRepository;

    @Autowired
    UserRepository userRepository;

    @Autowired
    ResourceLoader resourceLoader;

    @Autowired
    DishImageService imageValidationService;

    @Autowired
    DishesCacheService dishCacheService;

    @Autowired
    JwtRenewalService jwtRenewalService;

    @GetMapping("/GetDishes")
    public ResponseEntity<List<Dish>> getDishes() {
        try {
            List<Dish> dishes = dishCacheService.getDishes();
            if (dishes.isEmpty()) {
                logger.error("GetDishes: Could not find any dishes");
                return ResponseEntity.notFound().build();
            }
            logger.info("GetDishes: Returned all dishes. Length: {}", dishes.size());
            return new ResponseEntity<>(dishes, HttpStatus.OK);
        } catch (Exception e) {
            return ResponseEntity.internalServerError().build();
        }
    }

    @GetMapping("/GetDish/{id}")
    public ResponseEntity<Dish> getDish(@PathVariable("id") int id) {
        Dish foundDish = dishCacheService.getDish(id);
        if (foundDish != null) {
            logger.info("GetDish: Found Dish with id: {}", id);
            return new ResponseEntity<>(foundDish, HttpStatus.OK);
        }
        else {
            logger.error("GetDish: Dish with id: {} not found", id);
            return ResponseEntity.notFound().build();
        }
    }

    @PutMapping("/UpdateDish")
    public ResponseEntity<Void> updateDish(@RequestBody AddDishDTOWithId newDish) {
        Dish dishFound = dishCacheService.getDish(newDish.getDishId());
        if (dishFound == null) {
            logger.error("UpdateDish: Dish With ID: {} Not Found", newDish.getDishId());
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Dish With ID: " + newDish.getDishId() + " Not Found");
        }
        //get old image file
        String oldImageFileName = dishFound.getUrl();

        //get the base64 dish image data
        byte[] imageBytes = Base64.getDecoder().decode(newDish.getDishImageBase64());
        int[] imageBytesUnsigned = new int[imageBytes.length];
        for (int i=0; i<imageBytes.length; i++)
            imageBytesUnsigned[i] = imageBytes[i] & 0xFF;
        //some very basic validation (magic bytes)
        String extension = imageValidationService.IsValidImageMagicBytes(imageBytesUnsigned);
        if (extension == null)
        {
            logger.error("UpdateDish: Invalid image data");
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST,"Invalid Image Data");
        }
        String imageFileName = newDish.getDishName().trim().replace(' ', '_').toLowerCase() + "." + extension;
        //update the dish values with the ones provided from the request
        dishFound.setName(newDish.getDishName());
        dishFound.setDescription(newDish.getDishDescription());
        dishFound.setExtendedInfo(newDish.getDishExtendedInfo());
        dishFound.setPrice(newDish.getPrice());
        dishFound.setUrl(imageFileName);

        dishCacheService.updateCacheEntry(dishFound);
        dishRepository.save(dishFound);

        //delete OLD static file and create NEW image file
        try {
            Path staticFolderPath = Paths.get(resourceLoader.getResource("classpath:static").getURI());
            Path oldImageFilePath = staticFolderPath.resolve("dishimages/" + oldImageFileName);
            Path newImageFilePath = staticFolderPath.resolve("dishimages/" + imageFileName);
            Files.deleteIfExists(oldImageFilePath);
            Files.write(newImageFilePath, imageBytes);
        } catch (IOException ioe) {
            //it's OK, image file is not critical error
            logger.error("UpdateDish: Could not remove/update static image for dish");
        }

        return ResponseEntity.ok().build();
    }

    @PostMapping("/AddDish")
    public ResponseEntity<Integer> addDish(@RequestBody AddDishDTO newDish) {
        boolean found = dishCacheService.existDishWithoutId(newDish);
        if (found) {
            logger.error("AddDish: Dish already exists");
            throw new ResponseStatusException(HttpStatus.CONFLICT,"Dish Already Exists");
        }
        //base64 image data validation
        byte[] imageBytes = Base64.getDecoder().decode(newDish.getDishImageBase64());
        int[] imageBytesUnsigned = new int[imageBytes.length];
        for (int i=0; i<imageBytes.length; i++)
            imageBytesUnsigned[i] = imageBytes[i] & 0xFF;
        String extension = imageValidationService.IsValidImageMagicBytes(imageBytesUnsigned);
        if (extension == null)
        {
            logger.error("AddDish: Invalid image data");
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST,"Invalid Image Data");
        }
        //now insert the dish into the db and receive the DishID returned
        String imageFileName = newDish.getDishName().trim().replace(' ', '_').toLowerCase() + "." + extension;
        Dish newDishToAdd = new Dish(0, newDish.getDishName(), newDish.getDishDescription(), newDish.getPrice(), newDish.getDishExtendedInfo(), imageFileName);

        //save to db first (to get the fresh Id)
        newDishToAdd = dishRepository.save(newDishToAdd);
        dishCacheService.addCacheEntry(newDishToAdd);

        // put to static dishimages folder the image
        try {
            Path staticFolderPath = Paths.get(resourceLoader.getResource("classpath:static").getURI());
            Path imageFilePath = staticFolderPath.resolve("dishimages/" + imageFileName);
            // Write to file
            Files.write(imageFilePath, imageBytes);
        } catch (IOException ioe) {
            //it's OK, image file is not critical error
            logger.error("AddDish: Could not create a new static image for dish");
        }
        return ResponseEntity.ok(newDishToAdd.getId());
    }

    @DeleteMapping("/DeleteDish/{id}")
    public ResponseEntity<Dish> deleteDish(@PathVariable("id") int id) {
        Dish localDish = dishCacheService.getDish(id);
        if (localDish != null) {
            dishRepository.deleteById(id);
            dishCacheService.deleteCacheEntry(id);
            //delete static dish image file
            try {
                Path staticFolderPath = Paths.get(resourceLoader.getResource("classpath:static").getURI());
                Path imageFilePath = staticFolderPath.resolve("dishimages/" + localDish.getUrl());
                // delete image file
                Files.deleteIfExists(imageFilePath);
            } catch (IOException ioe) {
                //it's OK, image file is not critical error
                logger.error("DeleteDish: Could not delete the static image for dish");
            }
            return ResponseEntity.ok().build();
        } else {
            logger.error("DeleteDish: Dish with id: {} not found", id);
            return ResponseEntity.notFound().build();
        }
    }

    //Authorized, default scheme
    @PostMapping("/Order")
    public ResponseEntity<Void> createOrder(@RequestBody WebOrderDTO order) {
        logger.info("Order: Order received");
        if (order == null || order.order() == null || order.userId() == null || order.order().length == 0){
            //wrong input data, something bad happened on client side -> 400
            logger.error("Order: Wrong Order Data");
            return ResponseEntity.badRequest().build();
        }
        List<Dish> dishesFromDb = new ArrayList<>(dishRepository.findAll());
        if (dishesFromDb.isEmpty()) {
            //no dishes on our server! ->500
            logger.error("Order: NO dishes found in db!");
            return ResponseEntity.internalServerError().build();
        }

        List<Dish> dishesFromOrder = new ArrayList<>();
        BigDecimal cost = new BigDecimal("0", new MathContext(18)).setScale(2, RoundingMode.HALF_UP);
        for (WebOrderItemDTO item : order.order()) {
            String dishName = "";
            boolean idExistsInDb = false;
            for (Dish dish : dishesFromDb) {
                if (item.getDishId() == dish.getId()) {
                    idExistsInDb = true;
                    dishesFromOrder.add(dish);
                    dishName = dish.getName();
                    cost = cost.add(dish.getPrice().multiply(new BigDecimal(String.valueOf(item.getDishCounter()), new MathContext(18)).setScale(2, RoundingMode.HALF_UP)));
                    break;
                }
            }
            if (!idExistsInDb) {
                //404
                logger.error("Order: At least one Order Dish ID does not exist in db!");
                return ResponseEntity.notFound().build();
            }
            logger.info("Dish Id: {}, Dish NAME: {}, Dish Counter: {}", item.getDishId(), dishName, item.getDishCounter());
        }

        Optional<User> user = userRepository.findById(order.userId());
        if (user.isEmpty()) {
            logger.error("User with userId: {} not found in db", order.userId());
            return ResponseEntity.internalServerError().build(); //return 500? it is our error that user does not exist
        }
        Order orderToInsert = WebOrderDTOToEntity.OrderDTOToEntity(order, dishesFromOrder, cost, user.get());

        //insert to db
        orderRepository.save(orderToInsert);
        return ResponseEntity.ok().build();
    }

    //Authorized, default scheme
    @GetMapping("/GetUserOrders/{userId}")
    public ResponseEntity<UserOrdersDTO> getUserOrders(@RequestHeader(HttpHeaders.AUTHORIZATION) String token, @PathVariable("userId") String userId) {
        //TODO!
        if (token == null || !token.startsWith("Bearer ")) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        }
        try {
            // Extract the JWT token
            SignedJWT signedJWT = SignedJWT.parse(token.substring(7)); //skip "Bearer " prefix
            // Fetch the JWKS URL (cached)
            JWKSet jwkSet = jwtRenewalService.getJwkSet();
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
            // Check the 'sub' claim
            String subject = signedJWT.getJWTClaimsSet().getSubject();
            if (subject == null || !subject.equals(userId))
                return ResponseEntity.status(HttpStatus.FORBIDDEN).build();

        } catch (ParseException | JOSEException e) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        }

        //userId token check ok, we should retrieve this user's orders
        //TODO!

        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).build();
    }
}
