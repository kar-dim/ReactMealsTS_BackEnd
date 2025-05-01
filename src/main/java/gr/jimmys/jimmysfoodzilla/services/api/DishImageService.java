package gr.jimmys.jimmysfoodzilla.services.api;

import org.springframework.stereotype.Component;

@Component
public interface DishImageService {
    String IsValidImageMagicBytes (int[] imageData);
}
