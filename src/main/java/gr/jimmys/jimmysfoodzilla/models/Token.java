package gr.jimmys.jimmysfoodzilla.models;

import jakarta.persistence.*;

import java.sql.Timestamp;
import java.time.LocalDateTime;

@Entity
@Table(name = "Tokens")
public class Token {
    public Token() {

    }
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
    public Token(String tokenValue, String tokenType, LocalDateTime expiryDate) {
        this.tokenValue = tokenValue;
        this.tokenType = tokenType;
        this.expiryDate = expiryDate;
    }

    public int getId() {
        return Id;
    }

    public void setId(int id) {
        Id = id;
    }

    public String getTokenValue() {
        return tokenValue;
    }

    public void setTokenValue(String tokenValue) {
        this.tokenValue = tokenValue;
    }

    public String getTokenType() {
        return tokenType;
    }

    public void setTokenType(String tokenType) {
        this.tokenType = tokenType;
    }

    public LocalDateTime getExpiryDate() {
        return expiryDate;
    }

    public void setExpiryDate(LocalDateTime expiryDate) {
        this.expiryDate = expiryDate;
    }

}
