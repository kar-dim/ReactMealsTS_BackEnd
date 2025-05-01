package gr.jimmys.jimmysfoodzilla.security;

import jakarta.annotation.PostConstruct;
import kong.unirest.core.Unirest;
import kong.unirest.jackson.JacksonObjectMapper;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.PropertySource;

//Class that configures various application data
@Configuration
@PropertySource("file:${user.home}/.auth0/secret.properties")
public class AppConfig {
    @PostConstruct
    public void init() {
        Unirest.config().setObjectMapper(new JacksonObjectMapper());
    }
}
