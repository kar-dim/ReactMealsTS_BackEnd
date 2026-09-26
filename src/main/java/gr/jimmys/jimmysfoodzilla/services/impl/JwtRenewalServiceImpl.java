package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.services.api.JwtRenewalService;
import gr.jimmys.jimmysfoodzilla.services.api.JwtService;
import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

@Service
public class JwtRenewalServiceImpl implements JwtRenewalService {
    private final Logger logger = LoggerFactory.getLogger(JwtRenewalServiceImpl.class);

    private final JwtService jwtService;
    private static final DateTimeFormatter EXPIRY_FORMAT = DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm");

    private volatile String managementApiToken = "";
    private volatile boolean running = true;
    private Thread renewalThread;

    public JwtRenewalServiceImpl(JwtService jwtService) {
        this.jwtService = jwtService;
    }

    @PostConstruct
    public void init() {
        renewalThread = new Thread(this::renewalLoop, "m2m-token-renewal");
        renewalThread.setDaemon(true);
        renewalThread.start();
    }

    @jakarta.annotation.PreDestroy
    public void stop() {
        running = false;
        if (renewalThread != null) {
            renewalThread.interrupt();
        }
    }

    private void renewalLoop() {
        logger.info("M2M token renewal thread started");
        while (running) {
            try {
                logger.info("Retrieving local M2M token...");
                var token = jwtService.retrieveToken();
                if (token == null || token.getExpiryDate() == null || token.getExpiryDate().isBefore(LocalDateTime.now().plusSeconds(30))) {
                    logger.info("No token found in db, or it is expired/near-expiry, renewing...");
                    var newAccessToken = jwtService.renewToken();
                    if (newAccessToken == null) {
                        Thread.sleep(20 * 1000);
                        continue;
                    }
                    setManagementApiToken(newAccessToken.getTokenValue());
                    logger.info("Successfully renewed M2M token");
                    sleepUntilRefresh(newAccessToken.getExpiryDate());
                } else {
                    logger.info("M2M token valid, expires at: {}", token.getExpiryDate().format(EXPIRY_FORMAT));
                    setManagementApiToken(token.getTokenValue());
                    sleepUntilRefresh(token.getExpiryDate());
                }
            } catch (InterruptedException ie) {
                logger.warn("M2M renewal thread interrupted, stopping renewal");
                Thread.currentThread().interrupt();
                return;
            } catch (Exception ex) {
                logger.error("Unexpected error in M2M token renewal loop: {}", ex.getMessage(), ex);
                try {
                    Thread.sleep(10_000);
                } catch (InterruptedException ie) {
                    Thread.currentThread().interrupt();
                    return;
                }
            }
        }
    }

    // sleep until 30s before expiry; enforces minimum 5s pause to prevent CPU busy-spin
    private void sleepUntilRefresh(LocalDateTime expiryDate) throws InterruptedException {
        if (expiryDate == null) {
            Thread.sleep(5000);
            return;
        }
        var sleepTime = Duration.between(LocalDateTime.now(), expiryDate.minusSeconds(30));
        long millis = sleepTime.toMillis();
        if (millis <= 0) {
            Thread.sleep(5000);
        } else {
            Thread.sleep(millis);
        }
    }

    @Override
    public String getManagementApiToken() {
        return managementApiToken;
    }

    @Override
    public void setManagementApiToken(String value) {
        this.managementApiToken = value != null ? value : "";
    }
}
