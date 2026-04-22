package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.services.api.TunnelService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class NgrokTunnelServiceImpl implements TunnelService {
    private final Logger logger = LoggerFactory.getLogger(NgrokTunnelServiceImpl.class);

    @Value("${ngrok.url}")
    private String ngrokUrl;

    @Value("${server.port}")
    private int port;

    @Value("${isdevelopment}")
    private boolean isDev;

    @EventListener(ApplicationReadyEvent.class)
    @Override
    public void startTunnel() {
        if (!isDev)
            return;
        Thread ngrokThread = new Thread(() -> {
            try {
                logger.info("Killing any existing ngrok instances...");
                Process kill = new ProcessBuilder(List.of("taskkill", "/f", "/im", "ngrok.exe"))
                        .start();
                int killCode = kill.waitFor();
                if (killCode == 0)
                    logger.info("Existing ngrok instance terminated");
                else
                    logger.info("No existing ngrok instance found (taskkill exit: {})", killCode);

                logger.info("Starting ngrok tunnel on port {}...", port);
                Process ngrok = new ProcessBuilder(
                        List.of("ngrok", "http", "--domain=" + ngrokUrl, String.valueOf(port)))
                        .inheritIO()
                        .start();
                int exitCode = ngrok.waitFor();
                logger.warn("ngrok process exited with code {}", exitCode);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                logger.warn("ngrok thread interrupted");
            } catch (Exception e) {
                logger.error("Failed to start ngrok: {}", e.getMessage());
            }
        });
        ngrokThread.setDaemon(true);
        ngrokThread.setName("ngrok-tunnel");
        ngrokThread.start();
    }
}
