package gr.jimmys.jimmysfoodzilla.models.util;

import gr.jimmys.jimmysfoodzilla.dto.WebOrderDTO;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.models.Order;
import gr.jimmys.jimmysfoodzilla.models.OrderItem;
import gr.jimmys.jimmysfoodzilla.models.User;

import java.math.BigDecimal;
import java.util.List;
import java.util.stream.IntStream;

public class WebOrderDTOToEntity {
    public static Order orderDTOToEntity(WebOrderDTO orderDTO, List<Dish> dishesFromOrder, BigDecimal cost, User user) {
        Order newOrder = new Order();
        newOrder.setUser(user);
        newOrder.setTotalCost(cost);
        var orderItems = IntStream.range(0, orderDTO.order().length)
                .mapToObj(i -> new OrderItem(0, orderDTO.order()[i].getDishCounter(), newOrder, dishesFromOrder.get(i)))
                .toList();
        newOrder.setOrderItems(orderItems);
        return newOrder;
    }
}
