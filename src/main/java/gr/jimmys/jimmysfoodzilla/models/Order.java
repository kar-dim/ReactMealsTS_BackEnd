package gr.jimmys.jimmysfoodzilla.models;

import jakarta.persistence.*;

import java.math.BigDecimal;
import java.util.List;

@Entity
@Table(name = "Orders")
public class Order {

    public Order() {

    }
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private int Id;

    @Column(name="TotalCost", precision= 18, scale = 2)
    private BigDecimal totalCost;

    //User reference
    @ManyToOne
    @JoinColumn(name = "UserId", referencedColumnName = "User_Id")
    private User user;

    //OrderItem reference
    @OneToMany(mappedBy = "order", cascade = CascadeType.ALL, orphanRemoval = true)
    private List<OrderItem> orderItems;

    public int getId() {
        return Id;
    }

    public void setId(int id) {
        Id = id;
    }

    public BigDecimal getTotalCost() {
        return totalCost;
    }

    public void setTotalCost(BigDecimal totalCost) {
        this.totalCost = totalCost;
    }

    public User getUser() {
        return user;
    }

    public void setUser(User user) {
        this.user = user;
    }

    public List<OrderItem> getOrderItems() {
        return orderItems;
    }

    public void setOrderItems(List<OrderItem> orderItems) {
        this.orderItems = orderItems;
    }
}
