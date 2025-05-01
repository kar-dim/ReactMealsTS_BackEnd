package gr.jimmys.jimmysfoodzilla.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;
import lombok.Data;

@Data
@AllArgsConstructor
public class Auth0UserSerialize {
    @JsonProperty("email")
    private String email;

    @JsonProperty("user_metadata")
    private UserMetadata userMetadata;
}