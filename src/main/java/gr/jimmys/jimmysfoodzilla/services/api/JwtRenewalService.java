package gr.jimmys.jimmysfoodzilla.services.api;

import com.nimbusds.jose.jwk.JWKSet;
import org.springframework.http.HttpStatus;

public interface JwtRenewalService {
    void setJwkSet(JWKSet jwkSet);
    HttpStatus validateToken(String token, String audienceToCheck);
    String getManagementApiToken();
    void setManagementApiToken(String value);
}
