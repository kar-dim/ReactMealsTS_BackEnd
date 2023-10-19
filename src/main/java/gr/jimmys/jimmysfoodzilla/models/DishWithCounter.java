package gr.jimmys.jimmysfoodzilla.models;

import java.math.BigDecimal;

public class DishWithCounter extends Dish {
    private int Dish_counter;

    public DishWithCounter(String dish_name, String dish_description, BigDecimal price, String dish_extended_info, String dish_url, int dish_counter) {
        super(dish_name, dish_description, price, dish_extended_info, dish_url);
        Dish_counter = dish_counter;
    }

    public int getDish_counter() {
        return Dish_counter;
    }

    public void setDish_counter(int dish_counter) {
        Dish_counter = dish_counter;
    }
}
