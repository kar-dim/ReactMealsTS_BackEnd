package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.services.api.TunnelService;
import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
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

    @PostConstruct
    @Override
    public void startTunnel() {
        //don't run ngrok in production
        if (!isDev)
            return;
        Thread ngrokThread = new Thread(() -> {
            try {
                logger.info("START service");
                var ngrokKill = new ProcessBuilder(List.of("taskkill", "/f", "/im", "ngrok.exe"));
                var ngrokStart = new ProcessBuilder(List.of("ngrok", "http", "--domain=" + ngrokUrl, String.valueOf(port)));
                //kill (if exists)
                Process p = ngrokKill.start();
                int code = p.waitFor();
                int value = p.exitValue();
                if (code == 0 && value == 0) {
                    logger.info("TASKKILL successfully terminated ngrok instances");
                } //else don't care, no ngrok instances were killed
                //start
                ngrokStart.start();
                logger.info("STARTED successfully");
            } catch (Exception e) {
                logger.error("ERROR: {}", e.getMessage());
            }
        });
        ngrokThread.start();
    }
}
