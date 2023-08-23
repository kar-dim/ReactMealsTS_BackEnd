package gr.jimmys.jimmysfoodzilla.models;
import jakarta.persistence.*;

import java.math.BigDecimal;

@Entity
@Table(name = "Dishes")
public class Dish
{
    public Dish() {
    }

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name="DishId")
    private int DishId;

    @Column(name=" Dish_name", columnDefinition = "VARCHAR(MAX)")
    private String Dish_name;

    @Column(name=" Dish_description", columnDefinition = "VARCHAR(MAX)")
    private String Dish_description;

    @Column(name="Price", precision= 18, scale = 2)
    private BigDecimal Price;

    @Column(name=" Dish_extended_info", columnDefinition = "VARCHAR(MAX)")
    private String Dish_extended_info;

    @Column(name=" Dish_url",columnDefinition = "VARCHAR(MAX)")
    private String Dish_url;

    public int getDishId() {
        return DishId;
    }

    public void setDishId(int dishId) {
        DishId = dishId;
    }

    public String getDish_name() {
        return Dish_name;
    }

    public void setDish_name(String dish_name) {
        Dish_name = dish_name;
    }

    public String getDish_description() {
        return Dish_description;
    }

    public void setDish_description(String dish_description) {
        Dish_description = dish_description;
    }

    public BigDecimal getPrice() {
        return Price;
    }

    public void setPrice(BigDecimal price) {
        Price = price;
    }

    public String getDish_extended_info() {
        return Dish_extended_info;
    }

    public void setDish_extended_info(String dish_extended_info) {
        Dish_extended_info = dish_extended_info;
    }

    public String getDish_url() {
        return Dish_url;
    }

    public void setDish_url(String dish_url) {
        Dish_url = dish_url;
    }

    public Dish(String dish_name, String dish_description, BigDecimal price, String dish_extended_info, String dish_url) {
        Dish_name = dish_name;
        Dish_description = dish_description;
        Price = price;
        Dish_extended_info = dish_extended_info;
        Dish_url = dish_url;
    }
}

class DishWithCounter extends Dish  {
    private int Dish_counter;

    public DishWithCounter(String dish_name, String dish_description, BigDecimal price, String dish_extended_info, String dish_url, int dish_counter){
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
