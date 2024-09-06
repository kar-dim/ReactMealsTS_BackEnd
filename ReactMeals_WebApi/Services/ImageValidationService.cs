namespace ReactMeals_WebApi.Services;

public class ImageValidationService
{
    private static readonly Dictionary<byte[], string> knownMagicBytes = new()
    {
        { new byte[] { 0xFF, 0xD8, 0xFF }, "jpg" },
        { new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "png" },
        { new byte[] { 0x47, 0x49, 0x46, 0x38 }, "gif" },
        { new byte[] { 0x42, 0x4D}, "bmp" }
    };
    public string RetrieveImageExtension(byte[] imageData)
    {
        if (imageData.Length < 32) //too small
            return null;
        ReadOnlySpan<byte> imageSpan = imageData;
        foreach (var magicByte in knownMagicBytes)
        {
            if (imageSpan.Length >= magicByte.Key.Length && imageSpan[..magicByte.Key.Length].SequenceEqual(magicByte.Key))
                return magicByte.Value;
        }
        return null;
    }
}
