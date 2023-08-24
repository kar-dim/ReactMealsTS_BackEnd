package gr.jimmys.jimmysfoodzilla.repository;

import gr.jimmys.jimmysfoodzilla.models.Token;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;

public interface TokenRepository extends JpaRepository<Token, Integer> {
    @Query("SELECT t FROM Token t WHERE t.tokenType = :tokenType")
    List<Token> findAllManagementApiTokens(@Param(value="tokenType") String tokenType);
}
