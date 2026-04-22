package gr.jimmys.jimmysfoodzilla.security;

import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import org.springframework.context.annotation.PropertySource;
import org.springframework.security.oauth2.core.DelegatingOAuth2TokenValidator;
import org.springframework.security.oauth2.core.OAuth2Error;
import org.springframework.security.oauth2.core.OAuth2TokenValidatorResult;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtTypeValidator;
import org.springframework.security.oauth2.jwt.JwtValidators;
import org.springframework.security.oauth2.jwt.NimbusJwtDecoder;

import java.net.http.HttpClient;
import java.time.Duration;

@Configuration
@PropertySource("file:${user.home}/.auth0/secret.properties")
public class AppConfig {

    // shared HTTP client for all outbound REST calls
    @Bean
    public HttpClient httpClient() {
        return HttpClient.newBuilder()
                .connectTimeout(Duration.ofSeconds(10))
                .build();
    }

    /**
     * Primary JwtDecoder for user tokens, must be @Primary because defining any JwtDecoder causes
     * the m2mJwtDecoder to be used for user tokens, causing 401 (wrong audience).
     * Uses withIssuerLocation so the JWKS URI is discovered (automatically) from auth0 configuration
     */
    @Bean
    @Primary
    public JwtDecoder userJwtDecoder(
            @Value("${spring.security.oauth2.resourceserver.jwt.issuer-uri}") String issuerUri,
            @Value("${auth0.audience}") String audience) {
        NimbusJwtDecoder decoder = NimbusJwtDecoder
                .withIssuerLocation(issuerUri)
                .build();
        decoder.setJwtValidator(new DelegatingOAuth2TokenValidator<>(
                JwtValidators.createDefaultWithIssuer(issuerUri),
                JwtTypeValidator.jwt(),
                token -> token.getAudience().contains(audience)
                        ? OAuth2TokenValidatorResult.success()
                        : OAuth2TokenValidatorResult.failure(
                                new OAuth2Error("invalid_token", "Invalid audience", null))
        ));
        return decoder;
    }

    /**
     * Secondary JwtDecoder for Auth0 M2M tokens (used by CreateUser, called from Auth0 Actions).
     * Uses a different audience than user tokens, NimbusJwtDecoder handles JWK auto-refresh.
     */
    @Bean
    @Qualifier("m2mJwtDecoder")
    public JwtDecoder m2mJwtDecoder(
            @Value("${auth0.domain}") String domain,
            @Value("${auth0.m2maudience}") String m2mAudience) {
        NimbusJwtDecoder decoder = NimbusJwtDecoder
                .withJwkSetUri("https://" + domain + "/.well-known/jwks.json")
                .build();
        decoder.setJwtValidator(new DelegatingOAuth2TokenValidator<>(
                JwtValidators.createDefaultWithIssuer("https://" + domain + "/"),
                JwtTypeValidator.jwt(),
                token -> token.getAudience().contains(m2mAudience)
                        ? OAuth2TokenValidatorResult.success()
                        : OAuth2TokenValidatorResult.failure(
                                new OAuth2Error("invalid_token", "Invalid audience", null))
        ));
        return decoder;
    }
}
