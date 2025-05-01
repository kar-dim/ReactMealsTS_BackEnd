package gr.jimmys.jimmysfoodzilla.repository;

import gr.jimmys.jimmysfoodzilla.dto.AllUserOrdersDTO;
import gr.jimmys.jimmysfoodzilla.models.Order;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;

public interface OrderRepository extends JpaRepository<Order, Integer> {
    @Query("SELECT new gr.jimmys.jimmysfoodzilla.dto.AllUserOrdersDTO(" +
            "o.totalCost, oi.id, o.id, d.id, oi.dishCounter, d.name, d.description, d.price) " +
            "FROM OrderItem oi " +
            "JOIN oi.order o " +
            "JOIN oi.dish d " +
            "WHERE o.user.id = :userId")
    List<AllUserOrdersDTO> findUserOrders(@Param("userId") String userId);
}