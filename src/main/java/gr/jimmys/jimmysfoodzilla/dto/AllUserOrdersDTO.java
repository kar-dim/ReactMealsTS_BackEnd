package gr.jimmys.jimmysfoodzilla.dto;

import java.math.BigDecimal;

public record AllUserOrdersDTO(
        BigDecimal totalCost,
        int orderItemId,
        int webOrderId,
        int dishId,
        int dishCounter,
        String dishName,
        String dishDescription,
        BigDecimal price
) {
}