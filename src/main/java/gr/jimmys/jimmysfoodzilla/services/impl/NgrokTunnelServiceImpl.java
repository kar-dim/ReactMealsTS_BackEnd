package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.services.api.TunnelService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Service;

import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Service;

import java.util.List;

@Profile("!test")
@Service
public class NgrokTunnelServiceImpl implements TunnelService {
    private final Logger logger = LoggerFactory.getLogger(NgrokTunnelServiceImpl.class);

    @Value("${ngrok.url}")
    private String ngrokUrl;

    @Value("${server.port}")
    private int port;

    @Value("${isdevelopment}")
    private boolean isDev;

    private volatile Process ngrokProcess;

    @EventListener(ApplicationReadyEvent.class)
    @Override
    public void startTunnel() {
        if (!isDev)
            return;
        Thread ngrokThread = new Thread(() -> {
            try {
                logger.info("Killing any existing ngrok instances...");
                Process kill = new ProcessBuilder(List.of("taskkill", "/f", "/im", "ngrok.exe"))
                        .redirectOutput(ProcessBuilder.Redirect.DISCARD)
                        .redirectError(ProcessBuilder.Redirect.DISCARD)
                        .start();
                kill.waitFor();

                logger.info("Starting ngrok tunnel on port {}...", port);
                ngrokProcess = new ProcessBuilder(
                        List.of("ngrok", "http", "--url=" + ngrokUrl, String.valueOf(port)))
                        .redirectOutput(ProcessBuilder.Redirect.DISCARD)
                        .redirectError(ProcessBuilder.Redirect.DISCARD)
                        .start();
                int exitCode = ngrokProcess.waitFor();
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

    @jakarta.annotation.PreDestroy
    public void stopTunnel() {
        if (ngrokProcess != null && ngrokProcess.isAlive()) {
            logger.info("Stopping ngrok process...");
            ngrokProcess.destroyForcibly();
        }
    }
}
