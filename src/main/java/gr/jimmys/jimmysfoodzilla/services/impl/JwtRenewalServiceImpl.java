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

    private String managementApiToken;

    public JwtRenewalServiceImpl() {
        setManagementApiToken("");
    }

    @PostConstruct
    public void init() {
        Thread renewalThread = new Thread(() -> {
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
                        var sleepTime = Duration.between(LocalDateTime.now(), newAccessToken.getExpiryDate().minusSeconds(30));
                        setManagementApiToken(newAccessToken.getTokenValue());
                        logger.info("Successfully renewed M2M token");
                        Thread.sleep(sleepTime.toMillis());
                    } else {
                        logger.info("M2M token valid, expires at: {}", token.getExpiryDate().format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm")));
                        setManagementApiToken(token.getTokenValue());
                        var sleepTime = Duration.between(LocalDateTime.now(), token.getExpiryDate().minusSeconds(30));
                        if (!sleepTime.isNegative() && !sleepTime.isZero())
                            Thread.sleep(sleepTime.toMillis());
                    }
                } catch (InterruptedException ie) {
                    logger.error("M2M renewal thread INTERRUPTED! New tokens won't be received!");
                }
            }
        });
        renewalThread.setDaemon(true);
        renewalThread.start();
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
