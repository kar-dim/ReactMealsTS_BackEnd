package gr.jimmys.jimmysfoodzilla;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.web.servlet.config.annotation.CorsRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

@SpringBootApplication
public class JimmysFoodzillaApplication {
    public static void main(String[] args) {
        SpringApplication.run(JimmysFoodzillaApplication.class, args);
    }

    @Bean
    public WebMvcConfigurer corsConfigurer() {
        return new WebMvcConfigurer() {
            @Override
            public void addCorsMappings(CorsRegistry registry) {
                registry.addMapping("/**").allowedMethods("HEAD", "GET", "PUT", "POST", "DELETE", "PATCH").allowedOrigins("http://localhost:3000", "https://react-meals-ts-front-end.vercel.app").allowedHeaders("X-Requested-With", "Content-Type", "Authorization", "ngrok-skip-browser-warning"); //3000
            }
        };
    }

}
