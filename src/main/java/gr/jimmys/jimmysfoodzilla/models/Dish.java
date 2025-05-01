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
@Table(name = "Dishes")
public class Dish
{
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name="DishId")
    private int id;

    @Column(name=" Dish_name", columnDefinition = "VARCHAR(MAX)")
    private String name;

    @Column(name=" Dish_description", columnDefinition = "VARCHAR(MAX)")
    private String description;

    @Column(name="Price", precision= 18, scale = 2)
    private BigDecimal price;

    @Column(name=" Dish_extended_info", columnDefinition = "VARCHAR(MAX)")
    private String extendedInfo;

    @Column(name=" Dish_url",columnDefinition = "VARCHAR(MAX)")
    private String url;

    @Override
    public boolean equals(Object o){
        if (o == this)
            return true;
        if (!(o instanceof Dish d))
            return false;
        return this.id == d.getId();
    }
    @Override
    public int hashCode() {
        return this.id; //why not? ID -> distinct value..
    }
}

