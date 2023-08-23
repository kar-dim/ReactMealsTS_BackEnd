package gr.jimmys.jimmysfoodzilla.DTO;

import java.math.BigDecimal;

public class AddDishDTO {
    private String Dish_name;
    private String Dish_description;
    public BigDecimal Price;
    private String Dish_extended_info;
    private String Dish_image_base64; //base64 encoded image sent from client

    public AddDishDTO() {

    }
    public AddDishDTO(String dish_name, String dish_description, BigDecimal price, String dish_extended_info, String dish_image_base64) {
        Dish_name = dish_name;
        Dish_description = dish_description;
        Price = price;
        Dish_extended_info = dish_extended_info;
        Dish_image_base64 = dish_image_base64;
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

    public String getDish_image_base64() {
        return Dish_image_base64;
    }

    public void setDish_image_base64(String dish_image_base64) {
        Dish_image_base64 = dish_image_base64;
    }
}
