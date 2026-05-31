package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.services.api.JwtRenewalService;
import gr.jimmys.jimmysfoodzilla.services.api.JwtService;
import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

@Service
public class JwtRenewalServiceImpl implements JwtRenewalService {
    private final Logger logger = LoggerFactory.getLogger(JwtRenewalServiceImpl.class);

    @Autowired
    private JwtService jwtService;

    private static final DateTimeFormatter EXPIRY_FORMAT = DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm");

    private String managementApiToken;

    public JwtRenewalServiceImpl() {
        setManagementApiToken("");
    }

    @PostConstruct
    public void init() {
        Thread renewalThread = new Thread(this::renewalLoop, "m2m-token-renewal");
        renewalThread.setDaemon(true);
        renewalThread.start();
    }

    private void renewalLoop() {
        logger.info("M2M token renewal thread started");
        while (true) {
            try {
                logger.info("Retrieving local M2M token...");
                var token = jwtService.retrieveToken();
                if (token == null || token.getExpiryDate().isBefore(LocalDateTime.now())) {
                    logger.info("No token found in db, or it is expired, renewing...");
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
            }
        }
    }

    // sleep until 30s before expiry, returns immediately if that moment has already passed
    private void sleepUntilRefresh(LocalDateTime expiryDate) throws InterruptedException {
        var sleepTime = Duration.between(LocalDateTime.now(), expiryDate.minusSeconds(30));
        if (!sleepTime.isNegative() && !sleepTime.isZero())
            Thread.sleep(sleepTime.toMillis());
    }

    @Override
    public synchronized String getManagementApiToken() {
        return managementApiToken;
    }

    @Override
    public synchronized void setManagementApiToken(String value) {
        this.managementApiToken = value;
    }
}
