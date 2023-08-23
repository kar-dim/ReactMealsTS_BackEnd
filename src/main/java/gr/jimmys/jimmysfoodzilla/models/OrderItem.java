package gr.jimmys.jimmysfoodzilla.models;

import jakarta.persistence.*;

@Entity
@Table(name = "OrderItems")
public class OrderItem {

    public OrderItem() {

    }
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private int Id;

    @Column(name="Dish_counter")
    private int dishCounter;

    //ORDER reference
    @ManyToOne(cascade = CascadeType.ALL)
    @JoinColumn(name="OrderId")
    private Order order;

    //DISH reference
    @ManyToOne(cascade = CascadeType.ALL)
    @JoinColumn(name="DishId", referencedColumnName = "DishId")
    private Dish dish;

    public OrderItem(int dishCounter, Order order, Dish dish) {
        this.dishCounter = dishCounter;
        this.order = order;
        this.dish = dish;
    }

    public int getId() {
        return Id;
    }

    public void setId(int id) {
        Id = id;
    }

    public int getDishCounter() {
        return dishCounter;
    }

    public void setDishCounter(int dishCounter) {
        this.dishCounter = dishCounter;
    }

    public Order getOrder() {
        return order;
    }

    public void setOrder(Order order) {
        this.order = order;
    }

    public Dish getDish() {
        return dish;
    }

    public void setDish(Dish dish) {
        this.dish = dish;
    }
}
