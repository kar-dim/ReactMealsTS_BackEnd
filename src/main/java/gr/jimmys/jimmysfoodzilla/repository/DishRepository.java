package gr.jimmys.jimmysfoodzilla.repository;

import gr.jimmys.jimmysfoodzilla.models.Dish;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.math.BigDecimal;
import java.util.List;

public interface DishRepository extends JpaRepository<Dish, Integer> {

    @Query("SELECT d FROM Dish d WHERE " +
            "d.Dish_name = :Dish_name AND " +
            "d.Dish_description = :Dish_description AND " +
            "d.Price = :Price AND " +
            "d.Dish_extended_info = :Dish_extended_info" )
    List<Dish> existDishWithoutId(
            @Param("Dish_name") String dish_name,
            @Param("Dish_description") String dish_description,
            @Param("Price") BigDecimal price,
            @Param("Dish_extended_info") String dish_extended_info);

    @Query("SELECT d FROM Dish d ORDER BY d.DishId ASC")
    List<Dish> findAllAscendingById();
}