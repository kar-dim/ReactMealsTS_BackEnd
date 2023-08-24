package gr.jimmys.jimmysfoodzilla.DTO;

public class Auth0UserSerialize {
    private String email;
    private UserMetadata user_metadata;

    public Auth0UserSerialize(String email, UserMetadata user_metadata) {
        this.email = email;
        this.user_metadata = user_metadata;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
    }

    public UserMetadata getUser_metadata() {
        return user_metadata;
    }

    public void setUser_metadata(UserMetadata user_metadata) {
        this.user_metadata = user_metadata;
    }
}