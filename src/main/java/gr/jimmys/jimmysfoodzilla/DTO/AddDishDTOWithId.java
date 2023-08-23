package gr.jimmys.jimmysfoodzilla.DTO;

public class AddDishDTOWithId extends AddDishDTO {
    private int DishId;
    public AddDishDTOWithId() {

    }
    public AddDishDTOWithId(int dishid) {
        this.DishId = dishid;
    }

    public int getDishId() {
        return this.DishId;
    }
    public void setDishId(int dishId) {
        this.DishId = dishId;
    }
}
