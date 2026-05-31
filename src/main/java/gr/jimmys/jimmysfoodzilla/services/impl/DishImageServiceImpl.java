package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.services.api.DishImageService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Arrays;
import java.util.List;

@Service
public class DishImageServiceImpl implements DishImageService {
    private final Logger logger = LoggerFactory.getLogger(DishImageServiceImpl.class);

    private static final Path IMAGE_FOLDER = Paths.get("uploads", "dishimages");

    private record MagicBytes(byte[] signature, String extension) {}

    private static final List<MagicBytes> KNOWN_MAGIC_BYTES = List.of(
            new MagicBytes(new byte[]{(byte) 0xFF, (byte) 0xD8, (byte) 0xFF}, "jpg"),
            new MagicBytes(new byte[]{(byte) 0x89, (byte) 0x50, (byte) 0x4E, (byte) 0x47}, "png"),
            new MagicBytes(new byte[]{(byte) 0x47, (byte) 0x49, (byte) 0x46, (byte) 0x38}, "gif"),
            new MagicBytes(new byte[]{(byte) 0x42, (byte) 0x4D}, "bmp")
    );

    @Override
    public String validateImage(byte[] imageData) {
        if (imageData == null)
            return null;
        for (MagicBytes magic : KNOWN_MAGIC_BYTES) {
            byte[] sig = magic.signature();
            if (imageData.length >= sig.length
                    && Arrays.compare(sig, 0, sig.length, imageData, 0, sig.length) == 0)
                return magic.extension();
        }
        return null;
    }

    private Path safeResolve(String fileName) {
        // Prevent path traversal: resolve then verify the result is still inside IMAGE_FOLDER
        Path resolved = IMAGE_FOLDER.toAbsolutePath().normalize()
                .resolve(Paths.get(fileName).getFileName()); // getFileName() strips any directory components
        if (!resolved.startsWith(IMAGE_FOLDER.toAbsolutePath().normalize()))
            throw new SecurityException("Path traversal attempt detected: " + fileName);
        return resolved;
    }

    @Override
    public void deleteImage(String fileName) {
        try {
            Files.deleteIfExists(safeResolve(fileName));
        } catch (IOException ioe) {
            logger.error("Could not remove file with name: {}", fileName);
        }
    }

    @Override
    public void saveImage(String fileName, byte[] data) {
        try {
            Files.write(safeResolve(fileName), data);
        } catch (IOException e) {
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
