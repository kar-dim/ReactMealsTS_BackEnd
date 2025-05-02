package gr.jimmys.jimmysfoodzilla.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class AddDishDTO {
    @JsonProperty("dish_name")
    private String dishName;

    @JsonProperty("dish_description")
    private String dishDescription;

    @JsonProperty("price")
    public BigDecimal price;

    @JsonProperty("dish_extended_info")
    private String dishExtendedInfo;

    @JsonProperty("dish_image_base64")
    private String dishImageBase64;
}
