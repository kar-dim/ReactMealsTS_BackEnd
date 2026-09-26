package gr.jimmys.jimmysfoodzilla.models;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;

@NoArgsConstructor
@AllArgsConstructor
@Data
@Entity
@Table(name = "OrderItems")
public class OrderItem {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private int id;

    @Column(name = "Dish_counter")
    private int dishCounter;

    // Snapshot fields preserved at order time
    @Column(name = "Dish_name", columnDefinition = "VARCHAR(MAX)")
    private String dishName;

    @Column(name = "Dish_description", columnDefinition = "VARCHAR(MAX)")
    private String dishDescription;

    @Column(name = "Price", precision = 18, scale = 2)
    private BigDecimal price;

    //ORDER reference
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "OrderId")
    private Order order;

    //DISH reference
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "dish_id")
    private Dish dish;

    public OrderItem(int id, int dishCounter, Order order, Dish dish, String dishName, String dishDescription, BigDecimal price) {
        this.id = id;
        this.dishCounter = dishCounter;
        this.order = order;
        this.dish = dish;
        this.dishName = dishName;
        this.dishDescription = dishDescription;
        this.price = price;
    }
}
