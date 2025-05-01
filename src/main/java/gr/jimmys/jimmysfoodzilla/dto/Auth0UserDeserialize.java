package gr.jimmys.jimmysfoodzilla.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class Auth0UserDeserialize {

    @JsonProperty("email")
    private String email;

    @JsonProperty("name")
    private String name;

    @JsonProperty("nickname")
    private String nickname;

    @JsonProperty("user_id")
    private String userId;

    @JsonProperty("user_metadata")
    private UserMetadata userMetadata;

    public boolean isValidUser() {
        return email != null && !email.isEmpty()
                && userId != null && !userId.isEmpty()
                && userMetadata != null
                && userMetadata.getName() != null && !userMetadata.getName().isEmpty()
                && userMetadata.getLastName() != null && !userMetadata.getLastName().isEmpty()
                && userMetadata.getAddress() != null && !userMetadata.getAddress().isEmpty();
    }
}
