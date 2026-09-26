using ReactMeals_WebApi.Services.Interfaces;

namespace ReactMeals_WebApi.Services.Implementations
{
    public class DishImageService(ILogger<DishImageService> logger, IConfiguration configuration, IHostEnvironment environment) : IDishImageService
    {
        private readonly string _imagePath = Path.GetFullPath(configuration["Images:Directory"] ?? "Images", environment.ContentRootPath);
        private static readonly (byte[] Magic, string Extension)[] knownMagicBytes =
        [
            (new byte[] { 0xFF, 0xD8, 0xFF }, "jpg"),
            (new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "png"),
            (new byte[] { 0x47, 0x49, 0x46, 0x38 }, "gif"),
            (new byte[] { 0x42, 0x4D }, "bmp")
        ];

        public string ValidateImage(byte[] imageData)
        {
            if (imageData.Length < 32)
                return null;
            ReadOnlySpan<byte> imageSpan = imageData;
            foreach (var (magic, extension) in knownMagicBytes)
            {
                if (imageSpan.Length >= magic.Length && imageSpan[..magic.Length].SequenceEqual(magic))
                    return extension;
            }
            return null;
        }

        public void DeleteImage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return;
            try 
            {
                File.Delete(Path.Combine(_imagePath, fileName));
            }
            catch (Exception ex)
            {
                logger.LogError("Could not delete image {FileName}: {Error}", fileName, ex.Message);
            }
        }

        public void SaveImage(string fileName, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Invalid image filename", nameof(fileName));
            Directory.CreateDirectory(_imagePath);
            using var stream = new FileStream(Path.Combine(_imagePath, fileName), FileMode.CreateNew, FileAccess.Write);
            stream.Write(data);
        }

        public void ReplaceImage(string oldFile, string newFile, byte[] data)
        {
            SaveImage(newFile, data);
            DeleteImage(oldFile);
        }
    }
}
