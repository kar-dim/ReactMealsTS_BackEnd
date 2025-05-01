package gr.jimmys.jimmysfoodzilla.models.util;

import gr.jimmys.jimmysfoodzilla.dto.WebOrderDTO;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.models.Order;
import gr.jimmys.jimmysfoodzilla.models.OrderItem;
import gr.jimmys.jimmysfoodzilla.models.User;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

public class WebOrderDTOToEntity {
    public static Order orderDTOToEntity(WebOrderDTO orderDTO, List<Dish> dishesFromOrder, BigDecimal cost, User user) {
        Order newOrder = new Order();
        newOrder.setUser(user);
        newOrder.setTotalCost(cost);
        List<OrderItem> orderItems = new ArrayList<>();
        for (int i = 0; i< orderDTO.order().length; i++) {
            OrderItem orderItem = new OrderItem();
            orderItem.setDishCounter(orderDTO.order()[i].getDishCounter());
            orderItem.setDish(dishesFromOrder.get(i));
            orderItem.setOrder(newOrder); // Associate the Order with the OrderItem
            orderItems.add(orderItem);
        }
        newOrder.setOrderItems(orderItems);
        return newOrder;
    }
}
