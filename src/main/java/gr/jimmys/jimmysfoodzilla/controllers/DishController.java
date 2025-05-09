package gr.jimmys.jimmysfoodzilla.controllers;

import gr.jimmys.jimmysfoodzilla.common.ErrorMessages;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTOWithId;
import gr.jimmys.jimmysfoodzilla.dto.UserOrdersDTO;
import gr.jimmys.jimmysfoodzilla.dto.WebOrderDTO;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.services.api.DishService;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import gr.jimmys.jimmysfoodzilla.services.api.JwtRenewalService;
import gr.jimmys.jimmysfoodzilla.services.api.OrderService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;

import java.util.List;

import static gr.jimmys.jimmysfoodzilla.common.ErrorMessages.*;

@RestController
@RequestMapping("/api/Dishes")
public class DishController {
    private final Logger logger = LoggerFactory.getLogger(DishController.class);

    @Value("${auth0.audience}")
    private String audience;

    @Autowired
    DishService dishService;

    @Autowired
    OrderService orderService;

    @Autowired
    DishesCacheService cache;

    @Autowired
    JwtRenewalService jwtRenewalService;

    @GetMapping("/GetDish/{id}")
    public ResponseEntity<Dish> getDish(@PathVariable("id") int id) {
        var foundDish = cache.getDish(id);
        if (foundDish == null) {
            logger.error("GetDish: Dish with id: {} not found", id);
            return ResponseEntity.notFound().build();
        }
        logger.info("GetDish: Found Dish with id: {}", id);
        return new ResponseEntity<>(foundDish, HttpStatus.OK);
    }

    @GetMapping("/GetDishes")
    public ResponseEntity<List<Dish>> getDishes() {
        try {
            var foundDishes = cache.getDishes();
            if (foundDishes.isEmpty()) {
                logger.error("GetDishes: Could not find any dishes");
                return ResponseEntity.notFound().build();
            }
            logger.info("GetDishes: Returned all dishes. Length: {}", foundDishes.size());
            return new ResponseEntity<>(foundDishes, HttpStatus.OK);
        } catch (Exception e) {
            return ResponseEntity.internalServerError().build();
        }
    }

    @PostMapping("/AddDish")
    public ResponseEntity<Integer> addDish(@RequestBody AddDishDTO newDish) {
        var result = dishService.addDish(newDish);
        if (!result.isSuccess()) {
            logger.error("AddDish failed: {}", result.error());
            switch(result.error()){
               case CONFLICT:
                    throw new ResponseStatusException(HttpStatus.CONFLICT, CONFLICT);
                case BAD_DISH_PRICE_REQUEST:
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, BAD_DISH_PRICE_REQUEST);
                case BAD_DISH_NAME_REQUEST:
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, BAD_DISH_NAME_REQUEST);
                default:
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, BAD_REQUEST);
            }
        }
        var dish = (Dish) result.ResultValue();
        return ResponseEntity.ok(dish.getId());
    }

    @PutMapping("/UpdateDish")
    public ResponseEntity<Void> updateDish(@RequestBody AddDishDTOWithId dto) {
        var result = dishService.updateDish(dto);
        if (!result.isSuccess()) {
            logger.error("UpdateDish failed: {}", result.error());
            switch(result.error()){
                case NOT_FOUND:
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, NOT_FOUND);
                case BAD_DISH_PRICE_REQUEST:
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, BAD_DISH_PRICE_REQUEST);
                default:
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, BAD_REQUEST);
            }
        }
        return ResponseEntity.ok().build();
    }

    @DeleteMapping("/DeleteDish/{id}")
    public ResponseEntity<Dish> deleteDish(@PathVariable("id") int id) {
        var result = dishService.deleteDish(id);
        return result.isSuccess() ? ResponseEntity.ok().build() : ResponseEntity.notFound().build();
    }

    //Authorized, default scheme
    @PostMapping("/Order")
    public ResponseEntity<Void> createOrder(@RequestBody WebOrderDTO dto) {
        var result = orderService.createOrder(dto);
        if (!result.isSuccess()) {
            logger.error("CreateOrder: {}", result.error());
            ResponseEntity.badRequest().build();
        }
        return ResponseEntity.ok().build();
    }

    //Authorized, default scheme
    @GetMapping("/GetUserOrders/{userId}")
    public ResponseEntity<UserOrdersDTO> getUserOrders(@RequestHeader(HttpHeaders.AUTHORIZATION) String token, @PathVariable("userId") String userId) {
        var validationResult = jwtRenewalService.validateToken(token, audience);
        if (validationResult != HttpStatus.OK)
            return ResponseEntity.status(validationResult).build();
        //userId token check ok, we should retrieve this user's orders
        return ResponseEntity.ok(orderService.getUserOrders(userId));
    }
}
