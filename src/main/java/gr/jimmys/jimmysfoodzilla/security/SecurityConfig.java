package gr.jimmys.jimmysfoodzilla.security;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configuration.WebSecurityCustomizer;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationConverter;
import org.springframework.security.oauth2.server.resource.authentication.JwtGrantedAuthoritiesConverter;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.web.cors.CorsConfiguration;
import org.springframework.web.cors.CorsConfigurationSource;
import org.springframework.web.cors.UrlBasedCorsConfigurationSource;

import java.util.List;

@Configuration
@EnableWebSecurity
public class SecurityConfig {

    @Autowired
    private JwtDecoder userJwtDecoder;

    private static final List<String> ALLOWED_ORIGINS = List.of(
            "http://localhost:3000",
            "https://react-meals-ts-front-end.vercel.app"
    );
    private static final List<String> ALLOWED_METHODS = List.of(
            "HEAD", "GET", "PUT", "POST", "DELETE", "PATCH"
    );
    private static final List<String> ALLOWED_HEADERS = List.of(
            "X-Requested-With", "Content-Type", "Authorization", "ngrok-skip-browser-warning"
    );

    @Bean
    public CorsConfigurationSource corsConfigurationSource() {
        CorsConfiguration config = new CorsConfiguration();
        config.setAllowedOrigins(ALLOWED_ORIGINS);
        config.setAllowedMethods(ALLOWED_METHODS);
        config.setAllowedHeaders(ALLOWED_HEADERS);

        UrlBasedCorsConfigurationSource source = new UrlBasedCorsConfigurationSource();
        source.registerCorsConfiguration("/**", config);
        return source;
    }

    @Bean
    public JwtAuthenticationConverter jwtAuthenticationConverter() {
        JwtGrantedAuthoritiesConverter converter = new JwtGrantedAuthoritiesConverter();
        converter.setAuthoritiesClaimName("permissions");
        converter.setAuthorityPrefix("");
        JwtAuthenticationConverter jwtConverter = new JwtAuthenticationConverter();
        jwtConverter.setJwtGrantedAuthoritiesConverter(converter);
        return jwtConverter;
    }

    @Bean
    public WebSecurityCustomizer webSecurityCustomizer() {
        return (web) -> web.ignoring().requestMatchers("/api/Users/CreateUser");
    }

    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http
                // Stateless JWT API — CSRF protection does not apply
                .csrf(csrf -> csrf.disable())
                // cors() picks up the CorsConfigurationSource bean above
                .cors(Customizer.withDefaults())
                .authorizeHttpRequests(requests -> requests
                        // Public endpoints
                        .requestMatchers(HttpMethod.GET, "/api/Dishes/GetDishes").permitAll()
                        .requestMatchers(HttpMethod.GET, "/api/Dishes/GetDish/**").permitAll()
                        // Authenticated user endpoints
                        .requestMatchers(HttpMethod.POST, "/api/Dishes/Order").authenticated()
                        .requestMatchers(HttpMethod.GET, "/api/Dishes/GetUserOrders/**").authenticated()
                        // Admin-only endpoints
                        .requestMatchers(HttpMethod.POST, "/api/Dishes/AddDish").hasAuthority("admin:admin")
                        .requestMatchers(HttpMethod.DELETE, "/api/Dishes/DeleteDish/**").hasAuthority("admin:admin")
                        .requestMatchers(HttpMethod.PUT, "/api/Dishes/UpdateDish").hasAuthority("admin:admin")
                        .requestMatchers(HttpMethod.DELETE, "/api/Users/DeleteUser/**").hasAuthority("admin:admin")
                        .requestMatchers(HttpMethod.PUT, "/api/Users/UpdateUser").hasAuthority("admin:admin")
                        .requestMatchers(HttpMethod.GET, "/api/Users/GetUsers").hasAuthority("admin:admin")
                        // Deny everything else — explicit allowlist is safer
                        .anyRequest().denyAll()
                )
                .oauth2ResourceServer(oauth2 -> oauth2
                        .jwt(jwt -> jwt
                                // Explicit decoder — prevents m2mJwtDecoder from being used here
                                .decoder(userJwtDecoder)
                                // Reads "permissions" claim instead of "scope" for authorities
                                .jwtAuthenticationConverter(jwtAuthenticationConverter())
                        )
                );
        return http.build();
    }
}
