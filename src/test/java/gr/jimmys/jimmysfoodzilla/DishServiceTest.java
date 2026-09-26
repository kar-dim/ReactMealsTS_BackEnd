package gr.jimmys.jimmysfoodzilla;

import gr.jimmys.jimmysfoodzilla.common.ErrorMessages;
import gr.jimmys.jimmysfoodzilla.common.Result;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTOWithId;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.repository.DishRepository;
import gr.jimmys.jimmysfoodzilla.repository.OrderRepository;
import gr.jimmys.jimmysfoodzilla.services.api.DishImageService;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import gr.jimmys.jimmysfoodzilla.services.impl.DishServiceImpl;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class DishServiceTest {

    @Mock
    private DishRepository dishRepository;

    @Mock
    private OrderRepository orderRepository;

    @Mock
    private DishesCacheService cache;

    @Mock
    private DishImageService imageService;

    private DishServiceImpl dishService;

    @BeforeEach
    void setUp() {
        dishService = new DishServiceImpl(dishRepository, orderRepository, cache, imageService);
    }

    @Test
    void updateDish_withoutNewImage_preservesExistingImage() {
        Dish existing = new Dish(1, "Burger", "Delicious", new BigDecimal("12.50"), "Info", "burger.jpg");
        when(cache.getDish(1)).thenReturn(existing);
        when(cache.getDishByName("Burger")).thenReturn(existing);
        when(dishRepository.save(any(Dish.class))).thenAnswer(inv -> inv.getArgument(0));

        AddDishDTOWithId dto = new AddDishDTOWithId("Burger", "Updated description", new BigDecimal("13.00"), "Info", null, 1);
        Result<Void> result = dishService.updateDish(dto);

        assertThat(result.isSuccess()).isTrue();
        verify(imageService, never()).saveImage(any(), any());
        verify(cache).updateCacheEntry(argThat(d -> "burger.jpg".equals(d.getUrl()) && "Burger".equals(d.getName())));
    }

    @Test
    void updateDish_withConflictingName_returnsConflict() {
        Dish existing = new Dish(1, "Burger", "Desc", new BigDecimal("10.00"), "Info", "burger.jpg");
        Dish other = new Dish(2, "Pizza", "Desc", new BigDecimal("15.00"), "Info", "pizza.jpg");
        when(cache.getDish(1)).thenReturn(existing);
        when(cache.getDishByName("Pizza")).thenReturn(other);

        AddDishDTOWithId dto = new AddDishDTOWithId("Pizza", "Desc", new BigDecimal("10.00"), "Info", null, 1);
        Result<Void> result = dishService.updateDish(dto);

        assertThat(result.isSuccess()).isFalse();
        assertThat(result.error()).isEqualTo(ErrorMessages.CONFLICT);
    }

    @Test
    void deleteDish_whenDishHasOrders_returnsConflict() {
        Dish existing = new Dish(1, "Burger", "Desc", new BigDecimal("10.00"), "Info", "burger.jpg");
        when(cache.getDish(1)).thenReturn(existing);
        when(orderRepository.hasOrders(1)).thenReturn(true);

        Result<Void> result = dishService.deleteDish(1);

        assertThat(result.isSuccess()).isFalse();
        assertThat(result.error()).isEqualTo(ErrorMessages.CONFLICT);
        verify(dishRepository, never()).delete(any());
        verify(cache, never()).deleteCacheEntry(anyInt());
    }

    @Test
    void deleteDish_whenNoOrders_deletesDishAndUnusedImage() {
        Dish existing = new Dish(1, "Burger", "Desc", new BigDecimal("10.00"), "Info", "burger.jpg");
        when(cache.getDish(1)).thenReturn(existing);
        when(orderRepository.hasOrders(1)).thenReturn(false);
        when(dishRepository.existsByUrl("burger.jpg")).thenReturn(false);

        Result<Void> result = dishService.deleteDish(1);

        assertThat(result.isSuccess()).isTrue();
        verify(dishRepository).delete(existing);
        verify(cache).deleteCacheEntry(1);
        verify(imageService).deleteImage("burger.jpg");
    }
}
