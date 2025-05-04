package gr.jimmys.jimmysfoodzilla.models;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;
import java.util.List;

@Data
@NoArgsConstructor
@AllArgsConstructor
@Entity
@Table(name = "Orders")
public class Order {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private int Id;

    @Column(name = "TotalCost", precision = 18, scale = 2)
    private BigDecimal totalCost;

    //User reference
    @ManyToOne
    @JoinColumn(name = "UserId", referencedColumnName = "User_Id")
    private User user;

    //OrderItem reference
    @OneToMany(mappedBy = "order", cascade = CascadeType.ALL, orphanRemoval = true)
    private List<OrderItem> orderItems;
}
