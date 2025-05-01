package gr.jimmys.jimmysfoodzilla.services.api;

import org.springframework.stereotype.Component;

@Component
public interface DishImageService {
    String validateImage(byte[] imageData);

    void deleteImage(String fileName);

    void saveImage(String fileName, byte[] data);

    void replaceImage(String oldFile, String newFile, byte[] data);
}
