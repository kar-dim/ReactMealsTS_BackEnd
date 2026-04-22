package gr.jimmys.jimmysfoodzilla.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class WebOrderItemDTO {
    @Min(1)
    @JsonProperty("dishid")
    private int dishId;

    @Min(1)
    @Max(100)
    @JsonProperty("dish_counter")
    private int dishCounter;
}
