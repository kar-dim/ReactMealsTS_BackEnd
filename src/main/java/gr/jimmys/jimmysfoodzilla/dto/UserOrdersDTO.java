package gr.jimmys.jimmysfoodzilla.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;

@AllArgsConstructor
public class UserOrdersDTO
{
    @JsonProperty("orders")
    public UserOrder[] orders;
}
