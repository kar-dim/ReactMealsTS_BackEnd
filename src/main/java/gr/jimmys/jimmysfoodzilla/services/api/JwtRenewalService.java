package gr.jimmys.jimmysfoodzilla.services.api;

import com.nimbusds.jose.jwk.JWKSet;
import org.springframework.http.HttpStatus;

public interface JwtRenewalService {
    JWKSet getJwkSet();
    void setJwkSet(JWKSet jwkSet);
    HttpStatus validateToken(String token);
    String getManagementApiToken();
    void setManagementApiToken(String value);
}
