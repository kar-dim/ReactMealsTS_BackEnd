package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.controllers.DishController;
import gr.jimmys.jimmysfoodzilla.services.api.DishImageService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.core.io.ResourceLoader;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;

@Component
public class DishImageServiceImpl implements DishImageService {
    private final Logger logger = LoggerFactory.getLogger(DishImageServiceImpl.class);

    private static final String DISH_IMAGES_PATH = "dishimages/";

    @Autowired
    ResourceLoader resourceLoader;

    private static final HashMap<byte[], String> knownMagicBytes;
    static {
        knownMagicBytes = new HashMap<>();
        knownMagicBytes.put(new byte[] { (byte)0xFF, (byte)0xD8, (byte)0xFF }, "jpg");
        knownMagicBytes.put(new byte[] { (byte)0x89, (byte)0x50, (byte)0x4E, (byte)0x47 }, "png");
        knownMagicBytes.put(new byte[] { (byte)0x47, (byte)0x49, (byte)0x46, (byte)0x38 }, "gif");
        knownMagicBytes.put(new byte[] { (byte)0x42, (byte)0x4D }, "bmp");
    }

    @Override
    public String validateImage(byte[] imageData) {
        for (Map.Entry<byte[], String> entry : knownMagicBytes.entrySet()) {
            if (Arrays.compare(entry.getKey(), 0, entry.getKey().length, imageData, 0, entry.getKey().length) == 0)
                return entry.getValue();
        }
        return null;
    }

    @Override
    public void deleteImage(String fileName) {
        try {
            var staticFolderPath = Paths.get(resourceLoader.getResource("classpath:static").getURI());
            Files.deleteIfExists(staticFolderPath.resolve(DISH_IMAGES_PATH + fileName));
        } catch (IOException ioe) {
            //it's OK, image file is not critical error
            logger.error("Could not remove file with name: {}", fileName);
        }
    }

    @Override
    public void saveImage(String fileName, byte[] data) {
        try {
            var staticFolderPath = Paths.get(resourceLoader.getResource("classpath:static").getURI());
            Files.write(staticFolderPath.resolve(DISH_IMAGES_PATH + fileName), data);
        } catch (IOException e) {
            //it's OK, image file is not critical error
            logger.error("Could not create static image file with file name: {}", fileName);
        }
    }

    @Override
    public void replaceImage(String oldFile, String newFile, byte[] data) {
        //delete OLD static file and create NEW image file
        deleteImage(oldFile);
        saveImage(newFile, data);
    }
}
