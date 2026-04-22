package gr.jimmys.jimmysfoodzilla.services.api;

public interface JwtRenewalService {
    String getManagementApiToken();
    void setManagementApiToken(String value);
}
