package gr.jimmys.jimmysfoodzilla.models;

import lombok.Data;
import lombok.EqualsAndHashCode;

import java.math.BigDecimal;

@EqualsAndHashCode(callSuper = true)
@Data
public class DishWithCounter extends Dish {
    private int dishCounter;

    public DishWithCounter(String dishName, String desc, BigDecimal price, String extInfo, String url, int dishCounter) {
        super(0, dishName, desc, price, extInfo, url);
        this.dishCounter = dishCounter;
    }
}
