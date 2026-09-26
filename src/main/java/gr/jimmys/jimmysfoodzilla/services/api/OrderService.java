package gr.jimmys.jimmysfoodzilla.services.api;

import gr.jimmys.jimmysfoodzilla.common.Result;
import gr.jimmys.jimmysfoodzilla.dto.UserOrdersDTO;
import gr.jimmys.jimmysfoodzilla.dto.WebOrderDTO;

public interface OrderService {
    Result<Void> createOrder(WebOrderDTO dto, String userId);
    UserOrdersDTO getUserOrders(String userId);
}
