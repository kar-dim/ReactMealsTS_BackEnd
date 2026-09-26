package gr.jimmys.jimmysfoodzilla.repository;

import gr.jimmys.jimmysfoodzilla.dto.AllUserOrdersDTO;
import gr.jimmys.jimmysfoodzilla.models.Order;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;

public interface OrderRepository extends JpaRepository<Order, Integer> {
    @Query("SELECT new gr.jimmys.jimmysfoodzilla.dto.AllUserOrdersDTO(" +
            "o.totalCost, oi.id, o.id, COALESCE(oi.dish.id, 0), oi.dishCounter, " +
            "COALESCE(oi.dishName, d.name), COALESCE(oi.dishDescription, d.description), COALESCE(oi.price, d.price)) " +
            "FROM OrderItem oi " +
            "JOIN oi.order o " +
            "LEFT JOIN oi.dish d " +
            "WHERE o.user.userId = :userId " +
            "ORDER BY o.id DESC, oi.id ASC")
    List<AllUserOrdersDTO> findUserOrders(@Param("userId") String userId);

    @Query("SELECT COUNT(oi) > 0 FROM OrderItem oi WHERE oi.dish.id = :dishId")
    boolean hasOrders(@Param("dishId") int dishId);
}