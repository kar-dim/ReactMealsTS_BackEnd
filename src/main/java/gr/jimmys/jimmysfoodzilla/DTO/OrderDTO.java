package gr.jimmys.jimmysfoodzilla.DTO;

import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.models.Order;
import gr.jimmys.jimmysfoodzilla.models.OrderItem;
import gr.jimmys.jimmysfoodzilla.models.User;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

public class OrderDTO {
    private OrderItemDTO[] order;
    private String UserId;

    public OrderDTO() {

    }

    public OrderDTO(OrderItemDTO[] order, String userId) {
        this.order = order;
        UserId = userId;
    }

    public OrderItemDTO[] getOrder() {
        return order;
    }

    public void setOrder(OrderItemDTO[] order) {
        this.order = order;
    }

    public String getUserId() {
        return UserId;
    }

    public void setUserId(String userId) {
        UserId = userId;
    }

    public static Order OrderDTOToEntity(OrderDTO orderDTO, List<Dish> dishesFromOrder, BigDecimal cost, User user) {

        Order newOrder = new Order();
        newOrder.setUser(user);
        newOrder.setTotalCost(cost);
        List<OrderItem> orderItems = new ArrayList<>();
        for (int i=0; i<orderDTO.order.length; i++) {
            OrderItem orderItem = new OrderItem();
            orderItem.setDishCounter(orderDTO.order[i].getDish_counter());
            orderItem.setDish(dishesFromOrder.get(i));
            orderItem.setOrder(newOrder); // Associate the Order with the OrderItem
            orderItems.add(orderItem);
        }
        newOrder.setOrderItems(orderItems);
        return newOrder;
    }
}
