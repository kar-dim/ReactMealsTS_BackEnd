package gr.jimmys.jimmysfoodzilla.controllers;

import gr.jimmys.jimmysfoodzilla.common.Result;
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
    private static final Logger logger = LoggerFactory.getLogger(DishController.class);

    private final DishService dishService;
    private final OrderService orderService;
    private final DishesCacheService cache;

    public DishController(DishService dishService, OrderService orderService, DishesCacheService cache) {
        this.dishService = dishService;
        this.orderService = orderService;
        this.cache = cache;
    }

    @GetMapping("/GetDish/{id}")
    public ResponseEntity<Dish> getDish(@PathVariable("id") int id) {
        var foundDish = cache.getDish(id);
        if (foundDish == null) {
            logger.error("GetDish: Dish with id: {} not found", id);
            return ResponseEntity.notFound().build();
        }
        logger.info("GetDish: Found Dish with id: {}", id);
        return ResponseEntity.ok(foundDish);
    }

    @GetMapping("/GetDishes")
    public ResponseEntity<List<Dish>> getDishes() {
        var foundDishes = cache.getDishes();
        logger.info("GetDishes: Returned all dishes. Length: {}", foundDishes.size());
        return ResponseEntity.ok(foundDishes);
    }

    @PostMapping("/AddDish")
    public ResponseEntity<Integer> addDish(@Valid @RequestBody AddDishDTO newDish) {
        Result<Dish> result = dishService.addDish(newDish);
        if (!result.isSuccess()) {
            logger.error("AddDish failed: {}", result.error());
            switch (result.error()) {
                case CONFLICT -> throw new ResponseStatusException(HttpStatus.CONFLICT, CONFLICT);
                case BAD_DISH_PRICE_REQUEST, BAD_DISH_NAME_REQUEST ->
                        throw new ResponseStatusException(HttpStatus.BAD_REQUEST, result.error());
                default -> throw new ResponseStatusException(HttpStatus.BAD_REQUEST, BAD_REQUEST);
            }
        }
        return ResponseEntity.ok(result.value().getId());
    }

    @PutMapping("/UpdateDish")
    public ResponseEntity<Void> updateDish(@Valid @RequestBody AddDishDTOWithId dto) {
        Result<Void> result = dishService.updateDish(dto);
        if (!result.isSuccess()) {
            logger.error("UpdateDish failed: {}", result.error());
            switch (result.error()) {
                case NOT_FOUND, BAD_UPDATE_DISH_REQUEST ->
                        throw new ResponseStatusException(HttpStatus.NOT_FOUND, BAD_UPDATE_DISH_REQUEST);
                case CONFLICT -> throw new ResponseStatusException(HttpStatus.CONFLICT, CONFLICT);
                case BAD_DISH_PRICE_REQUEST -> throw new ResponseStatusException(HttpStatus.BAD_REQUEST, BAD_DISH_PRICE_REQUEST);
                case BAD_DISH_NAME_REQUEST -> throw new ResponseStatusException(HttpStatus.BAD_REQUEST, BAD_DISH_NAME_REQUEST);
                default -> throw new ResponseStatusException(HttpStatus.BAD_REQUEST, BAD_REQUEST);
            }
        }
        return ResponseEntity.ok().build();
    }

    @DeleteMapping("/DeleteDish/{id}")
    public ResponseEntity<Void> deleteDish(@PathVariable("id") int id) {
        Result<Void> result = dishService.deleteDish(id);
        if (!result.isSuccess()) {
            if (CONFLICT.equals(result.error())) {
                throw new ResponseStatusException(HttpStatus.CONFLICT, "Dish belongs to an existing order");
            }
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, NOT_FOUND);
        }
        return ResponseEntity.ok().build();
    }

    @PostMapping("/Order")
    public ResponseEntity<Void> createOrder(@AuthenticationPrincipal Jwt jwt, @Valid @RequestBody WebOrderDTO dto) {
        String userId = jwt != null ? jwt.getSubject() : null;
        if (userId == null || userId.isBlank()) {
            logger.warn("CreateOrder: Missing or unauthenticated user");
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, UNAUTHORIZED);
        }
        Result<Void> result = orderService.createOrder(dto, userId);
        if (!result.isSuccess()) {
            logger.error("CreateOrder failed: {}", result.error());
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, result.error());
        }
        return ResponseEntity.ok().build();
    }

    @GetMapping(value = {"/GetUserOrders", "/GetUserOrders/{userId}"})
    public ResponseEntity<UserOrdersDTO> getUserOrders(@AuthenticationPrincipal Jwt jwt,
                                                       @PathVariable(value = "userId", required = false) String pathUserId) {
        String callerId = jwt != null ? jwt.getSubject() : null;
        if (callerId == null || callerId.isBlank()) {
            logger.warn("GetUserOrders: Missing or unauthenticated caller");
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, UNAUTHORIZED);
        }
        if (pathUserId != null && !pathUserId.isBlank() && !callerId.equals(pathUserId)) {
            logger.warn("GetUserOrders: path userId [{}] does not match JWT subject [{}]", pathUserId, callerId);
            return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
        }
        return ResponseEntity.ok(orderService.getUserOrders(callerId));
    }
}
