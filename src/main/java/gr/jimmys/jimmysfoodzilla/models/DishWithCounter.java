package gr.jimmys.jimmysfoodzilla.models;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.EqualsAndHashCode;

import java.math.BigDecimal;

@AllArgsConstructor
@EqualsAndHashCode(callSuper = true)
@Data
public class DishWithCounter extends Dish {
    @JsonProperty("dish_counter")
    private int dishCounter;

    public DishWithCounter(int id, String dishName, String desc, BigDecimal price, String extInfo, String url, int dishCounter) {
        super(id, dishName, desc, price, extInfo, url);
        this.dishCounter = dishCounter;
    }

    public DishWithCounter(int id, String dishName, String desc, BigDecimal price, int dishCounter) {
        super(id, dishName, desc, price, null, null);
        this.dishCounter = dishCounter;
    }
}
