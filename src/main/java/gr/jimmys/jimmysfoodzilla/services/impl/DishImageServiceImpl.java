package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.services.api.DishImageService;
import org.springframework.stereotype.Component;

import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;

@Component
public class DishImageServiceImpl implements DishImageService {
    private static final HashMap<int[], String> knownMagicBytes;
    static {
        knownMagicBytes = new HashMap<>();
        knownMagicBytes.put(new int[] { 0xFF, 0xD8, 0xFF }, "jpg");
        knownMagicBytes.put(new int[] { 0x89, 0x50, 0x4E, 0x47 }, "png");
        knownMagicBytes.put(new int[] { 0x47, 0x49, 0x46, 0x38 }, "gif");
        knownMagicBytes.put(new int[] { 0x42, 0x4D }, "bmp");
    }

    @Override
    public String IsValidImageMagicBytes (int[] imageData) {
        for (Map.Entry<int[], String> entry : knownMagicBytes.entrySet()) {
            if (Arrays.compare(entry.getKey(), 0, entry.getKey().length, imageData, 0, entry.getKey().length) == 0)
                return entry.getValue();
        }
        return null;
    }
}
