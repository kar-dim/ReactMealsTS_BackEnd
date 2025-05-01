package gr.jimmys.jimmysfoodzilla.models;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.ToString;

@NoArgsConstructor
@AllArgsConstructor
@Data
@ToString
@Entity
@Table(name = "Users")
public class User {
    @JsonProperty("user_id")
    @Id
    @Column(name = "User_Id")
    private String userId; //"user_id" from auth0

    @Column(name = "Email", columnDefinition = "VARCHAR(MAX)")
    private String email; //"email" from auth0

    //data from custom auth0 user_metadata
    @Column(name = "Name", columnDefinition = "VARCHAR(MAX)")
    private String name; //"name"

    @Column(name = "LastName", columnDefinition = "VARCHAR(MAX)")
    private String lastName; //"last_name"

    @Column(name = "Address", columnDefinition = "VARCHAR(MAX)")
    private String address; //"address"

    public User(String userId) {
        this.userId = userId;
    }
}
