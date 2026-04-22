package gr.jimmys.jimmysfoodzilla.controllers;

import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTOWithId;
import gr.jimmys.jimmysfoodzilla.dto.UserOrdersDTO;
import gr.jimmys.jimmysfoodzilla.dto.WebOrderDTO;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.services.api.DishService;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import gr.jimmys.jimmysfoodzilla.services.api.OrderService;
import jakarta.validation.Valid;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;

import java.util.List;

import static gr.jimmys.jimmysfoodzilla.common.ErrorMessages.*;

@RestController
@RequestMapping("/api/Dishes")
public class DishController {
    private final Logger logger = LoggerFactory.getLogger(DishController.class);

    @Autowired
    DishService dishService;

    @Autowired
    OrderService orderService;

    @Autowired
    DishesCacheService cache;

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
        var foundDishes = cache.getDishes();
        logger.info("GetDishes: Returned all dishes. Length: {}", foundDishes.size());
        return new ResponseEntity<>(foundDishes, HttpStatus.OK);
    }

    @PostMapping("/AddDish")
    public ResponseEntity<Integer> addDish(@Valid @RequestBody AddDishDTO newDish) {
        var result = dishService.addDish(newDish);
        if (!result.isSuccess()) {
            logger.error("AddDish failed: {}", result.error());
            switch (result.error()) {
                case CONFLICT:
                    throw new ResponseStatusException(HttpStatus.CONFLICT, CONFLICT);
                case BAD_DISH_PRICE_REQUEST:
                case BAD_DISH_NAME_REQUEST:
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, result.error());
                default:
                    throw new ResponseStatusException(HttpStatus.BAD_REQUEST, BAD_REQUEST);
            }
        }
        var dish = (Dish) result.ResultValue();
        return ResponseEntity.ok(dish.getId());
    }

    @PutMapping("/UpdateDish")
    public ResponseEntity<Void> updateDish(@Valid @RequestBody AddDishDTOWithId dto) {
        var result = dishService.updateDish(dto);
        if (!result.isSuccess()) {
            logger.error("UpdateDish failed: {}", result.error());
            switch (result.error()) {
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

    @PostMapping("/Order")
    public ResponseEntity<Void> createOrder(@AuthenticationPrincipal Jwt jwt, @Valid @RequestBody WebOrderDTO dto) {
        if (!jwt.getSubject().equals(dto.userId())) {
            logger.warn("CreateOrder: userId in body [{}] does not match JWT subject [{}]", dto.userId(), jwt.getSubject());
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        }
        var result = orderService.createOrder(dto);
        if (!result.isSuccess()) {
            logger.error("CreateOrder: {}", result.error());
            return ResponseEntity.badRequest().build();
        }
        return ResponseEntity.ok().build();
    }

    @GetMapping("/GetUserOrders/{userId}")
    public ResponseEntity<UserOrdersDTO> getUserOrders(@AuthenticationPrincipal Jwt jwt, @PathVariable("userId") String userId) {
        // spring security already validated JWT (sig, exp, iss, aud),
        // we only need to verify the caller is requesting their own orders
        if (!jwt.getSubject().equals(userId)) {
            logger.warn("GetUserOrders: path userId [{}] does not match JWT subject [{}]", userId, jwt.getSubject());
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        }
        return ResponseEntity.ok(orderService.getUserOrders(userId));
    }
}
