package gr.jimmys.jimmysfoodzilla.models;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Data
@NoArgsConstructor
@AllArgsConstructor
@Entity
@Table(name = "Tokens")
public class Token {
    public static String MANAGEMENT_API = "M_API";

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private int Id;

    //base64 encoded
    @Column(name = "TokenValue", columnDefinition = "VARCHAR(MAX)")
    private String tokenValue;

    //"M_API" used for now only (Auth0 Management API token)
    @Column(name = "TokenType", columnDefinition = "VARCHAR(MAX)")
    private String tokenType;

    @Column(name = "ExpiryDate")
    private LocalDateTime expiryDate;
}
