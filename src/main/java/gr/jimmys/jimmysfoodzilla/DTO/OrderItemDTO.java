package gr.jimmys.jimmysfoodzilla.DTO;

public class OrderItemDTO {

    private int Dishid;
    private int Dish_counter;

    public OrderItemDTO() {

    }
    public OrderItemDTO( int dishId, int dish_counter)  {
        Dish_counter = dish_counter;
        Dishid = dishId;
    }

    public int getDishid() {
        return Dishid;
    }

    public void setDishid(int dishId) {
        Dishid = dishId;
    }

    public int getDish_counter() {
        return Dish_counter;
    }

    public void setDish_counter(int dish_counter) {
        Dish_counter = dish_counter;
    }
}
