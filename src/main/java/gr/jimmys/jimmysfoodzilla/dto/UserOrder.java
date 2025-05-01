package gr.jimmys.jimmysfoodzilla.dto;

import gr.jimmys.jimmysfoodzilla.models.DishWithCounter;

import java.math.BigDecimal;

public record UserOrder(int id, DishWithCounter[] dishes, BigDecimal totalCost) {
}