package gr.jimmys.jimmysfoodzilla.repository;

import gr.jimmys.jimmysfoodzilla.models.Dish;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.math.BigDecimal;
import java.util.List;

public interface DishRepository extends JpaRepository<Dish, Integer> {

    @Query("SELECT d FROM Dish d WHERE " +
            "d.name = :Dish_name AND " +
            "d.description = :Dish_description AND " +
            "d.price = :Price AND " +
            "d.extendedInfo = :Dish_extended_info")
    List<Dish> existDishWithoutId(
            @Param("Dish_name") String dish_name,
            @Param("Dish_description") String dish_description,
            @Param("Price") BigDecimal price,
            @Param("Dish_extended_info") String dish_extended_info);

    @Query("SELECT d FROM Dish d ORDER BY d.id ASC")
    List<Dish> findAllAscendingById();
}