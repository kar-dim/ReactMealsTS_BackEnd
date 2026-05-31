package gr.jimmys.jimmysfoodzilla.models;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@NoArgsConstructor
@AllArgsConstructor
@Data
@Entity
@Table(name = "OrderItems")
public class OrderItem {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private int Id;

    @Column(name = "Dish_counter")
    private int dishCounter;

    //ORDER reference
    @ManyToOne(cascade = CascadeType.ALL)
    @JoinColumn(name = "OrderId")
    private Order order;

    //DISH reference
    @ManyToOne(cascade = CascadeType.ALL)
    @JoinColumn(name = "dish_id") // references Dish's @Id (dish_id) by default
    private Dish dish;
}
