package gr.jimmys.jimmysfoodzilla.services;

import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class NgrokService {

    @Value("${ngrok.url}")
    private String ngrokUrl;

    @Value("${server.port}")
    private int port;

    @Value("${isdevelopment}")
    private boolean isDev;

    private final Logger logger = LoggerFactory.getLogger(NgrokService.class);
    public NgrokService() {

    }

    @PostConstruct
    public void init() {
        Thread ngrokThread = new Thread( () -> {
            try {
                //Thread.sleep(5000); //wait so intiialization happens (not needed when @PostConstruct is used, after constructor)
                //don't run ngrok in production
                if (!isDev)
                    return;
                logger.info("NgrokService: START service");
                ProcessBuilder ngrokKill = new ProcessBuilder(List.of("taskkill","/f", "/im", "ngrok.exe"));
                ProcessBuilder ngrokStart = new ProcessBuilder(List.of("ngrok","http", "--domain=" + ngrokUrl, String.valueOf(port) ));
                //kill (if exists)
                Process p = ngrokKill.start();
                int code = p.waitFor();
                int value = p.exitValue();
                if (code == 0 && value == 0) {
                    logger.info("NgrokService: TASKKILL successfully terminated ngrok instances");
                } //else don't care, no ngrok instances were killed
                //start
                p = ngrokStart.start();
                logger.info("NgrokService: STARTED successfully");
            } catch (Exception e) {
                logger.error("NgrokService ERROR: " + e.getMessage());
            }
        });
        ngrokThread.start();
    }
}
