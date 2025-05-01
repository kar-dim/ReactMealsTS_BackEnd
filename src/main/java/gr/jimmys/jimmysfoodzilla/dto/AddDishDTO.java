package gr.jimmys.jimmysfoodzilla.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import java.math.BigDecimal;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class AddDishDTO {
    @JsonProperty("Dish_name")
    private String dishName;

    @JsonProperty("Dish_description")
    private String dishDescription;

    @JsonProperty("Price")
    public BigDecimal price;

    @JsonProperty("Dish_extended_info")
    private String dishExtendedInfo;

    @JsonProperty("Dish_image_base64")
    private String dishImageBase64;
}
