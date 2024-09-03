namespace ReactMeals_WebApi.Services
{
    public interface IImageValidationService
    {
        public string IsValidImageMagicBytes(byte[] imageData);
    }
    public class ImageValidationService : IImageValidationService {

        private static readonly Dictionary<byte[], string> knownMagicBytes;
        static ImageValidationService() 
        {
            knownMagicBytes = new Dictionary<byte[], string>()
            {
                { new byte[] { 0xFF, 0xD8, 0xFF }, "jpg" },
                { new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "png" },
                { new byte[] { 0x47, 0x49, 0x46, 0x38 }, "gif" },
                { new byte[] { 0x42, 0x4D}, "bmp" }
            };
        }
        public string IsValidImageMagicBytes(byte[] imageData)
        {
            if (imageData.Length < 32) //too small
                return null;

            // Magic bytes for different image formats

            foreach (var magicByteExtensions in knownMagicBytes) 
            {
                //if pattern match -> return OK
                if (Enumerable.SequenceEqual(imageData.Take(magicByteExtensions.Key.Length), magicByteExtensions.Key))
                    return magicByteExtensions.Value;
            }

            return null;
        }
    }
}
