package gr.jimmys.jimmysfoodzilla.exception;

/**
 * Thrown when an Auth0 Management API call cannot be completed, controllers translate this into a HTTP 500
 */
public class Auth0ManagementException extends RuntimeException {
    public Auth0ManagementException(String message) {
        super(message);
    }
}
