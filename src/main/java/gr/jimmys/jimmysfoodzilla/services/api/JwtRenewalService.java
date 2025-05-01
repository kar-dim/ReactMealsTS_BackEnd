package gr.jimmys.jimmysfoodzilla.services.api;

import com.nimbusds.jose.jwk.JWKSet;

public interface JwtRenewalService {
    JWKSet getJwkSet();
    void setJwkSet(JWKSet jwkSet);
    String getManagementApiToken();
    void setManagementApiToken(String value);
}
