package gr.jimmys.jimmysfoodzilla.DTO;

public class UserMetadata {
    private String name;
    private String last_name;
    private String address;
    public UserMetadata() {}
    public UserMetadata(String name, String last_name, String address) {
        this.name = name;
        this.last_name = last_name;
        this.address = address;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getLast_name() {
        return last_name;
    }

    public void setLast_name(String last_name) {
        this.last_name = last_name;
    }

    public String getAddress() {
        return address;
    }

    public void setAddress(String address) {
        this.address = address;
    }
}
