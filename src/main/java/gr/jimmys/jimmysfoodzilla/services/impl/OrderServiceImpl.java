package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.common.Result;
import gr.jimmys.jimmysfoodzilla.dto.*;
import gr.jimmys.jimmysfoodzilla.models.DishWithCounter;
import gr.jimmys.jimmysfoodzilla.models.User;
import gr.jimmys.jimmysfoodzilla.models.util.WebOrderDTOToEntity;
import gr.jimmys.jimmysfoodzilla.repository.OrderRepository;
import gr.jimmys.jimmysfoodzilla.repository.UserRepository;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import gr.jimmys.jimmysfoodzilla.services.api.OrderService;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.util.*;
import java.util.stream.Collectors;

@Service
public class OrderServiceImpl implements OrderService {
    private static final BigDecimal MAX_ORDER_TOTAL = new BigDecimal("9999999999999999.99");

    private final OrderRepository orderRepository;
    private final UserRepository userRepository;
    private final DishesCacheService cache;

    public OrderServiceImpl(OrderRepository orderRepository,
                            UserRepository userRepository,
                            DishesCacheService cache) {
        this.orderRepository = orderRepository;
        this.userRepository = userRepository;
        this.cache = cache;
    }

    @Override
    @Transactional
    public Result<Void> createOrder(WebOrderDTO dto, String userId) {
        if (dto == null || dto.order() == null || dto.order().length == 0 || userId == null || userId.isBlank())
            return Result.failure("Invalid order data");

        for (var item : dto.order()) {
            if (item == null || item.getDishId() <= 0 || item.getDishCounter() <= 0)
                return Result.failure("Invalid order data");
        }

        Optional<User> user = userRepository.findById(userId);
        if (user.isEmpty())
            return Result.failure("Registered user was not found");

        // Group duplicate dish IDs into aggregated quantities
        Map<Integer, Integer> counterById = Arrays.stream(dto.order())
                .collect(Collectors.toMap(
                        WebOrderItemDTO::getDishId,
                        WebOrderItemDTO::getDishCounter,
                        Integer::sum,
                        LinkedHashMap::new
                ));

        var orderDishes = cache.getDishes(new ArrayList<>(counterById.keySet()));
        if (orderDishes.size() != counterById.size())
            return Result.failure("At least one DishId provided does not exist");

        BigDecimal totalCost = BigDecimal.ZERO;
        for (var dish : orderDishes) {
            if (dish.getPrice() == null || dish.getPrice().compareTo(BigDecimal.ZERO) <= 0)
                return Result.failure("Invalid DishId: " + dish.getId());
            int count = counterById.get(dish.getId());
            BigDecimal itemTotal = dish.getPrice().multiply(BigDecimal.valueOf(count));
            totalCost = totalCost.add(itemTotal);
        }

        if (totalCost.compareTo(MAX_ORDER_TOTAL) > 0)
            return Result.failure("Order total is too large");

        orderRepository.save(WebOrderDTOToEntity.orderDTOToEntity(orderDishes, counterById, totalCost, user.get()));
        return Result.success();
    }

    @Override
    public UserOrdersDTO getUserOrders(String userId) {
        if (userId == null || userId.isBlank())
            return new UserOrdersDTO(new UserOrder[]{});

        var orders = orderRepository.findUserOrders(userId);
        if (orders.isEmpty())
            return new UserOrdersDTO(new UserOrder[]{});

        // Use LinkedHashMap to preserve orderId descending sort order from query
        var userOrders = orders.stream()
                .collect(Collectors.groupingBy(AllUserOrdersDTO::webOrderId, LinkedHashMap::new, Collectors.toList()))
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
