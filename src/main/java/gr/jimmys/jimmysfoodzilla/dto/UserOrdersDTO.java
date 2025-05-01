package gr.jimmys.jimmysfoodzilla.dto;

import com.fasterxml.jackson.annotation.JsonProperty;

public class UserOrdersDTO
{
    @JsonProperty("orders")
    public UserOrder[] orders;
}
