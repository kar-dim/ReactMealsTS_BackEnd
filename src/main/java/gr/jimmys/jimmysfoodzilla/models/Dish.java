package gr.jimmys.jimmysfoodzilla.models;

import com.fasterxml.jackson.annotation.JsonProperty;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;
import java.util.Objects;

@NoArgsConstructor
@AllArgsConstructor
@Data
@Entity
@Table(name = "Dishes")
public class Dish {
    @JsonProperty("dishId")
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "DishId")
    private int id;

    @JsonProperty("dish_name")
    @Column(name = " Dish_name", columnDefinition = "VARCHAR(MAX)")
    private String name;

    @JsonProperty("dish_description")
    @Column(name = " Dish_description", columnDefinition = "VARCHAR(MAX)")
    private String description;

    @JsonProperty("price")
    @Column(name = "Price", precision = 18, scale = 2)
    private BigDecimal price;

    @JsonProperty("dish_extended_info")
    @Column(name = " Dish_extended_info", columnDefinition = "VARCHAR(MAX)")
    private String extendedInfo;

    @JsonProperty("dish_url")
    @Column(name = " Dish_url", columnDefinition = "VARCHAR(MAX)")
    private String url;

    @Override
    public boolean equals(Object o) {
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

    public boolean equalsExceptId(AddDishDTO dto) {
        return Objects.equals(name, dto.getDishName()) &&
                Objects.equals(url, dto.getDishImageBase64()) &&
                Objects.equals(description, dto.getDishDescription()) &&
                Objects.equals(extendedInfo, dto.getDishExtendedInfo());
    }
}

