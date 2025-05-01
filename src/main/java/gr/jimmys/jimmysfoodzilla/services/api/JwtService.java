package gr.jimmys.jimmysfoodzilla.services.api;

import gr.jimmys.jimmysfoodzilla.models.Token;

public interface JwtService {
    Token retrieveToken();
    Token renewToken();
}
