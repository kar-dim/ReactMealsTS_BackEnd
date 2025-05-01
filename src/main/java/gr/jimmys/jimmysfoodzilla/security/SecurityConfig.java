package gr.jimmys.jimmysfoodzilla.security;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configuration.WebSecurityCustomizer;
import org.springframework.security.web.SecurityFilterChain;

@Configuration
@EnableWebSecurity
public class SecurityConfig {

    @Bean
    public WebSecurityCustomizer webSecurityCustomizer() {
        // Route from Auth0 M2M user Register application (auth0 action)
        return (web) -> web.ignoring().requestMatchers("/api/Users/CreateUser");
    }

    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http.authorizeHttpRequests(requests -> requests
                .requestMatchers(HttpMethod.POST, "/api/Users/CreateUser").permitAll()
                //.requestMatchers(HttpMethod.OPTIONS, "/**").permitAll()
                // Whitelist routes that don't need authentication
                .requestMatchers(HttpMethod.GET, "/api/Dishes/GetDishes").permitAll()
                .requestMatchers(HttpMethod.GET, "/api/Dishes/GetDish/**").permitAll()
                // Routes that require authorization
                .requestMatchers(HttpMethod.POST, "/api/Dishes/Order").authenticated()
                .requestMatchers(HttpMethod.GET, "/api/Dishes/GetUserOrders/**").authenticated()
                // Routes that require authorization + authentication via claim "permissions" with value "admin:admin"
                .requestMatchers(HttpMethod.POST, "/api/Dishes/AddDish").authenticated()
                .requestMatchers(HttpMethod.DELETE, "/api/Dishes/DeleteDish/**").authenticated()
                .requestMatchers(HttpMethod.PUT, "/api/Dishes/UpdateDish").authenticated()
                .requestMatchers(HttpMethod.DELETE, "/api/Users/DeleteUser/**").authenticated()
                .requestMatchers(HttpMethod.PUT, "/api/Users/UpdateUser").authenticated()
                .requestMatchers(HttpMethod.GET, "/api/Users/GetUsers").authenticated()
                .requestMatchers(HttpMethod.POST, "/api/Dishes/AddDish").hasAuthority("admin:admin")
                .requestMatchers(HttpMethod.DELETE, "/api/Dishes/DeleteDish/**").hasAuthority("admin:admin")
                .requestMatchers(HttpMethod.PUT, "/api/Dishes/UpdateDish").hasAuthority("admin:admin")
                .requestMatchers(HttpMethod.DELETE, "/api/Users/DeleteUser/**").hasAuthority("admin:admin")
                .requestMatchers(HttpMethod.PUT, "/api/Users/UpdateUser").hasAuthority("admin:admin")
                .requestMatchers(HttpMethod.GET, "/api/Users/GetUsers").hasAuthority("admin:admin")
                //anything else
                .anyRequest().permitAll()).oauth2ResourceServer(oauth2ResourceServer ->
                oauth2ResourceServer
                        .jwt(jwt -> Customizer.withDefaults())
        );

        return http.build();
    }
}
