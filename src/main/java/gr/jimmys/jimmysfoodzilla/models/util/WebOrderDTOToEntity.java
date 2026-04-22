package gr.jimmys.jimmysfoodzilla.models.util;

import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.models.Order;
import gr.jimmys.jimmysfoodzilla.models.OrderItem;
import gr.jimmys.jimmysfoodzilla.models.User;

import java.math.BigDecimal;
import java.util.List;
import java.util.Map;

public class WebOrderDTOToEntity {
    public static Order orderDTOToEntity(List<Dish> dishesFromOrder, Map<Integer, Integer> counterById, BigDecimal cost, User user) {
        Order newOrder = new Order();
        newOrder.setUser(user);
        newOrder.setTotalCost(cost);
        var orderItems = dishesFromOrder.stream()
                .map(dish -> new OrderItem(0, counterById.get(dish.getId()), newOrder, dish))
                .toList();
        newOrder.setOrderItems(orderItems);
        return newOrder;
    }
}
