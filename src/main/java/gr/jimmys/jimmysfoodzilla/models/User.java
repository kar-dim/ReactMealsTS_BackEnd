package gr.jimmys.jimmysfoodzilla.models;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;

@Entity
@Table(name = "Users")
public class User {

    public User () {

    }

    public String getUser_Id() {
        return User_Id;
    }

    public void setUser_Id(String user_Id) {
        this.User_Id = user_Id;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getLastName() {
        return lastName;
    }

    public void setLastName(String lastName) {
        this.lastName = lastName;
    }

    public String getAddress() {
        return address;
    }

    public void setAddress(String address) {
        this.address = address;
    }

    public User(String User_id) {
        this.User_Id = User_id;
    }

    public User(String User_Id, String email, String name, String lastName, String address) {
        this.User_Id = User_Id;
        this.email = email;
        this.name = name;
        this.lastName = lastName;
        this.address = address;
    }

    @Id
    @Column(name = "User_Id")
    private String User_Id; //"user_id" from auth0

    @Column(name = "Email", columnDefinition = "VARCHAR(MAX)")
    private String email; //"email" from auth0

    //data from custom auth0 user_metadata
    @Column(name = "Name", columnDefinition = "VARCHAR(MAX)")
    private String name; //"name"
    @Column(name = "LastName", columnDefinition = "VARCHAR(MAX)")
    private String lastName; //"last_name"
    @Column(name = "Address", columnDefinition = "VARCHAR(MAX)")
    private String address; //"address"

    @Override
    public String toString()
    {
        return "User ID " + User_Id + " email: " + email + " Name: " + name + " Last Name: " + lastName + " Address: " + address + "\n";
    }
}
