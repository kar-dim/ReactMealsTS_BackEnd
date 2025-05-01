package gr.jimmys.jimmysfoodzilla.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;
import lombok.EqualsAndHashCode;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;

@EqualsAndHashCode(callSuper = true)
@Data
@NoArgsConstructor
public class AddDishDTOWithId extends AddDishDTO {
    @JsonProperty("dishId")
    private int dishId;

    public AddDishDTOWithId(String dishName, String desc, BigDecimal price, String extInfo, String imageBase64, int dishId) {
        super(dishName, desc, price, extInfo, imageBase64);
        this.dishId = dishId;
    }
}
