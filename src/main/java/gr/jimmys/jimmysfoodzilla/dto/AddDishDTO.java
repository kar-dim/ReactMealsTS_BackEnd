package gr.jimmys.jimmysfoodzilla.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.DecimalMax;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Positive;
import jakarta.validation.constraints.Size;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class AddDishDTO {
    @NotBlank
    @Size(max = 200)
    @JsonProperty("dish_name")
    private String dishName;

    @Size(max = 2000)
    @JsonProperty("dish_description")
    private String dishDescription;

    @NotNull
    @Positive
    @DecimalMax("256")
    @JsonProperty("price")
    public BigDecimal price;

    @Size(max = 5000)
    @JsonProperty("dish_extended_info")
    private String dishExtendedInfo;

    @NotBlank
    @JsonProperty("dish_image_base64")
    private String dishImageBase64;
}
