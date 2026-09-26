package gr.jimmys.jimmysfoodzilla;

import gr.jimmys.jimmysfoodzilla.common.Result;
import gr.jimmys.jimmysfoodzilla.dto.WebOrderDTO;
import gr.jimmys.jimmysfoodzilla.dto.WebOrderItemDTO;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.models.Order;
import gr.jimmys.jimmysfoodzilla.models.User;
import gr.jimmys.jimmysfoodzilla.repository.OrderRepository;
import gr.jimmys.jimmysfoodzilla.repository.UserRepository;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import gr.jimmys.jimmysfoodzilla.services.impl.OrderServiceImpl;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class OrderServiceTest {

    @Mock
    private OrderRepository orderRepository;

    @Mock
    private UserRepository userRepository;

    @Mock
    private DishesCacheService cache;

    private OrderServiceImpl orderService;

    @BeforeEach
    void setUp() {
        orderService = new OrderServiceImpl(orderRepository, userRepository, cache);
    }

    @Test
    void createOrder_consolidatesDuplicateItemsAndSavesSnapshots() {
        String userId = "auth0|12345";
        User user = new User(userId, "test@example.com", "John", "Doe", "Main St 1");
        when(userRepository.findById(userId)).thenReturn(Optional.of(user));

        Dish burger = new Dish(1, "Burger", "Tasty burger", new BigDecimal("10.00"), "Info", "burger.jpg");
        when(cache.getDishes(anyList())).thenReturn(List.of(burger));

        // Client sent two entries for the same dish (e.g. quantity 2 and quantity 3)
        WebOrderItemDTO[] items = new WebOrderItemDTO[]{
                new WebOrderItemDTO(1, 2),
                new WebOrderItemDTO(1, 3)
        };
        WebOrderDTO dto = new WebOrderDTO(items);

        Result<Void> result = orderService.createOrder(dto, userId);

        assertThat(result.isSuccess()).isTrue();

        ArgumentCaptor<Order> orderCaptor = ArgumentCaptor.forClass(Order.class);
        verify(orderRepository).save(orderCaptor.capture());

        Order saved = orderCaptor.getValue();
        assertThat(saved.getUser()).isEqualTo(user);
        // Total cost: 5 * 10.00 = 50.00
        assertThat(saved.getTotalCost()).isEqualByComparingTo(new BigDecimal("50.00"));
        assertThat(saved.getOrderItems()).hasSize(1);
        var item = saved.getOrderItems().getFirst();
        assertThat(item.getDishCounter()).isEqualTo(5);
        assertThat(item.getDishName()).isEqualTo("Burger");
        assertThat(item.getDishDescription()).isEqualTo("Tasty burger");
        assertThat(item.getPrice()).isEqualByComparingTo(new BigDecimal("10.00"));
    }
}
