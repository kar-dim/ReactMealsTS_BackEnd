package gr.jimmys.jimmysfoodzilla.DTO;

import gr.jimmys.jimmysfoodzilla.models.DishWithCounter;
import java.math.BigDecimal;

public class UserOrder {
    public int id;
    public DishWithCounter[] dishes;
    public BigDecimal totalCost;
}
