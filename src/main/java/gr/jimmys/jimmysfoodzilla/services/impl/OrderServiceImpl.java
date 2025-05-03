package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.common.Result;
import gr.jimmys.jimmysfoodzilla.dto.*;
import gr.jimmys.jimmysfoodzilla.models.User;
import gr.jimmys.jimmysfoodzilla.models.DishWithCounter;
import gr.jimmys.jimmysfoodzilla.models.util.WebOrderDTOToEntity;
import gr.jimmys.jimmysfoodzilla.repository.DishRepository;
import gr.jimmys.jimmysfoodzilla.repository.OrderRepository;
import gr.jimmys.jimmysfoodzilla.repository.UserRepository;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import gr.jimmys.jimmysfoodzilla.services.api.OrderService;
import jakarta.persistence.EntityManager;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import static java.math.BigDecimal.valueOf;
import static java.util.stream.Collectors.toMap;

@Service
public class OrderServiceImpl implements OrderService {
    private final Logger logger = LoggerFactory.getLogger(OrderServiceImpl.class);

    @Autowired
    OrderRepository orderRepository;

    @Autowired
    UserRepository userRepository;

    @Autowired
    DishRepository dishRepository;

    @Autowired
    DishesCacheService cache;

    private final EntityManager entityManager;

    public OrderServiceImpl(EntityManager entityManager) {
        this.entityManager = entityManager;
    }
    //Create the order, write to db
    @Override
    public Result createOrder(WebOrderDTO dto) {
        Optional<User> user;
        if (dto == null || dto.order() == null || dto.userId() == null || dto.order().length == 0 || (user = userRepository.findById(dto.userId())).isEmpty())
            return Result.failure("Invalid order data");

        Map<Integer, Integer> counterById = Arrays.stream(dto.order()).collect(toMap(WebOrderItemDTO::getDishId, WebOrderItemDTO::getDishCounter));
        var orderDishes = cache.getDishes(new ArrayList<>(counterById.keySet()), entityManager);
        if (orderDishes.size() != counterById.size())
            return Result.failure("At least one DishId provided does not exist");

        //calculate order's total cost by multiplying each order item (dish * counter) and summing them
        var totalCost = orderDishes.stream()
                .map(dish -> dish.getPrice().multiply(valueOf(counterById.getOrDefault(dish.getId(), 0))))
                .reduce(BigDecimal.ZERO, BigDecimal::add);
        orderRepository.save(WebOrderDTOToEntity.orderDTOToEntity(dto, orderDishes, totalCost, user.get()));
        return Result.success();
    }

    @Override
    public UserOrdersDTO getUserOrders(String userId) {
        var orders = orderRepository.findUserOrders(userId);
        if (orders.isEmpty())
            return new UserOrdersDTO(new UserOrder[]{});

        var userOrders = orders.stream()
                .collect(Collectors.groupingBy(AllUserOrdersDTO::webOrderId))
                .entrySet()
                .stream()
                .map(entry -> {
                    var orderId = entry.getKey();
                    var group = entry.getValue();
                    var dishes = group.stream()
                            .map(g -> new DishWithCounter(g.dishId(), g.dishName(), g.dishDescription(), g.price(), g.dishCounter()))
                            .toArray(DishWithCounter[]::new);
                    var totalCost = group.getFirst().totalCost();
                    return new UserOrder(orderId, dishes, totalCost);
                }).toArray(UserOrder[]::new);
        return new UserOrdersDTO(userOrders);
    }
}
