package gr.jimmys.jimmysfoodzilla.controllers;

import gr.jimmys.jimmysfoodzilla.DTO.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.DTO.AddDishDTOWithId;
import gr.jimmys.jimmysfoodzilla.DTO.OrderDTO;
import gr.jimmys.jimmysfoodzilla.DTO.OrderItemDTO;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.models.Order;
import gr.jimmys.jimmysfoodzilla.models.User;
import gr.jimmys.jimmysfoodzilla.repository.DishRepository;
import gr.jimmys.jimmysfoodzilla.repository.OrderRepository;
import gr.jimmys.jimmysfoodzilla.repository.UserRepository;
import gr.jimmys.jimmysfoodzilla.services.ImageValidationService;
import org.apache.coyote.Response;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.core.io.ResourceLoader;
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
import java.util.ArrayList;
import java.util.Base64;
import java.util.List;
import java.util.Optional;

@RestController
@RequestMapping("/api/Dishes")
public class DishController {

    Logger logger = LoggerFactory.getLogger(DishController.class);
    @Autowired
    DishRepository dishRepository;
    @Autowired
    OrderRepository orderRepository;
    @Autowired
    UserRepository userRepository;

    @Autowired
    ResourceLoader resourceLoader;

    @Autowired
    ImageValidationService imageValidationService;

    @GetMapping("/GetDishes")
    public ResponseEntity<List<Dish>> getDishes() {
        try {
            List<Dish> dishes = new ArrayList<>(dishRepository.findAll());
            if (dishes.isEmpty()) {
                logger.error("GetDishes: Could not find any dishes");
                return ResponseEntity.notFound().build();
            }
            logger.info("GetDishes: Returned all dishes. Length: " + dishes.size());
            return new ResponseEntity<>(dishes, HttpStatus.OK);
        } catch (Exception e) {
            return ResponseEntity.internalServerError().build();
        }
    }

    @GetMapping("/GetDish/{id}")
    public ResponseEntity<Dish> getDish(@PathVariable("id") int id) {
        Optional<Dish> dishFromDb = dishRepository.findById(id);
        if (dishFromDb.isPresent()) {
            logger.info("GetDish: Found Dish with id: " + id);
            return new ResponseEntity<>(dishFromDb.get(), HttpStatus.OK);
        } else {
            logger.error("GetDish: Dish with id: " + id + " not found");
            return ResponseEntity.notFound().build();
        }
    }

    @PutMapping("/UpdateDish")
    public ResponseEntity<Void> updateDish(@RequestBody AddDishDTOWithId newDish) {
        Optional<Dish> dishFromDb = dishRepository.findById(newDish.getDishId());
        //if it does not exist -> 404
        if (!dishFromDb.isPresent()) {
            logger.error("UpdateDish: Dish With ID: " + newDish.getDishId() + " Not Found");
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Dish With ID: " + newDish.getDishId() + " Not Found");
        }
        //get old image file
        String oldImageFileName = dishFromDb.get().getDish_url();

        //get the base64 dish image data
        byte[] imageBytes = Base64.getDecoder().decode(newDish.getDish_image_base64());
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
        String imageFileName = newDish.getDish_name().trim().replace(' ', '_').toLowerCase() + "." + extension;
        //update the dish from DB
        dishFromDb.get().setDish_name(newDish.getDish_name());
        dishFromDb.get().setDish_description(newDish.getDish_description());
        dishFromDb.get().setDish_extended_info(newDish.getDish_extended_info());
        dishFromDb.get().setPrice(newDish.getPrice());
        dishFromDb.get().setDish_url(imageFileName);
        dishRepository.save(dishFromDb.get());

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
        //search in db(if exists-> return 409 CONFLICT)
        //we don't have the ID yet, search by other parameters
        List<Dish> foundInDb = dishRepository.existDishWithoutId(
                        newDish.getDish_name(),
                        newDish.getDish_description(),
                        newDish.getPrice(),
                        newDish.getDish_extended_info());
        if (foundInDb.size() > 0) {
            logger.error("AddDish: Dish already exists");
            throw new ResponseStatusException(HttpStatus.CONFLICT,"Dish Already Exists");
        }
        //base64 image data validation
        byte[] imageBytes = Base64.getDecoder().decode(newDish.getDish_image_base64());
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
        String imageFileName = newDish.getDish_name().trim().replace(' ', '_').toLowerCase() + "." + extension;
        Dish newDishToAdd = new Dish(newDish.getDish_name(), newDish.getDish_description(), newDish.getPrice(), newDish.getDish_extended_info(), imageFileName);
        //save to db
        newDishToAdd = dishRepository.save(newDishToAdd);

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
        return ResponseEntity.ok(newDishToAdd.getDishId());
    }

    @DeleteMapping("/DeleteDish/{id}")
    public ResponseEntity<Dish> deleteDish(@PathVariable("id") int id) {
        Optional<Dish> dishFromDb = dishRepository.findById(id);
        if (dishFromDb.isPresent()) {
            String imageName = dishFromDb.get().getDish_url();
            dishRepository.deleteById(id);
            //delete static dish image file
            try {
                Path staticFolderPath = Paths.get(resourceLoader.getResource("classpath:static").getURI());
                Path imageFilePath = staticFolderPath.resolve("dishimages/" + dishFromDb.get().getDish_url());
                // Write to file
                Files.deleteIfExists(imageFilePath);
            } catch (IOException ioe) {
                //it's OK, image file is not critical error
                logger.error("DeleteDish: Could not delete the static image for dish");
            }
            return ResponseEntity.ok().build();
        } else {
            logger.error("DeleteDish: Dish with id: " + id + " not found");
            return ResponseEntity.notFound().build();
        }
    }

    //Authorized, default scheme
    @PostMapping("/Order")
    public ResponseEntity<Void> createOrder(@RequestBody OrderDTO order) {
        logger.info("Order: Order received");
        if (order == null || order.getOrder() == null || order.getUserId() == null || order.getOrder().length == 0){
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
        for (OrderItemDTO item : order.getOrder()) {
            String dishName = "";
            boolean idExistsInDb = false;
            for (Dish dish : dishesFromDb) {
                if (item.getDishid() == dish.getDishId()) {
                    idExistsInDb = true;
                    dishesFromOrder.add(dish);
                    dishName = dish.getDish_name();
                    cost = cost.add(dish.getPrice().multiply(new BigDecimal(String.valueOf(item.getDish_counter()), new MathContext(18)).setScale(2, RoundingMode.HALF_UP)));
                    break;
                }
            }
            if (!idExistsInDb) {
                //404
                logger.error("Order: At least one Order Dish ID does not exist in db!");
                return ResponseEntity.notFound().build();
            }
            logger.info("Dish Id: " + item.getDishid() + ", Dish NAME: " + dishName + ", Dish Counter: " + item.getDish_counter());
        }

        Optional<User> user = userRepository.findById(order.getUserId());
        if (!user.isPresent()) {
            logger.error("User with userId: "+order.getUserId() +" not found in db");
            return ResponseEntity.internalServerError().build(); //return 500? it is our error that user does not exist
        }
        Order orderToInsert = OrderDTO.OrderDTOToEntity(order, dishesFromOrder, cost, user.get());

        //insert to db
        orderRepository.save(orderToInsert);
        return ResponseEntity.ok().build();
    }


}
