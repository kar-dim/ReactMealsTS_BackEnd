﻿using ReactMeals_WebApi.Services.Interfaces;

namespace ReactMeals_WebApi.Services.Implementations
{
    public class DishImageService(ILogger<DishImageService> logger) : IDishImageService
    {
        private static readonly string ImagePath = "Images";
        private static readonly Dictionary<byte[], string> knownMagicBytes = new()
        {
            { new byte[] { 0xFF, 0xD8, 0xFF }, "jpg" },
            { new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "png" },
            { new byte[] { 0x47, 0x49, 0x46, 0x38 }, "gif" },
            { new byte[] { 0x42, 0x4D}, "bmp" }
        };

        //Very basic image validation by using magic bytes
        public string ValidateImage(byte[] imageData)
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

        public void DeleteImage(string fileName)
        {
            try 
            {
                File.Delete(Path.Combine(ImagePath, fileName));
            }
            catch (Exception ex)
            {
                logger.LogError("Could not delete image {FileName}: {Error}", fileName, ex.Message);
            }
        }

        public void SaveImage(string fileName, byte[] data)
        {
            try
            {
                File.WriteAllBytes(Path.Combine(ImagePath, fileName), data);
            }
            catch (Exception ex)
            {
                logger.LogError("Could not save image {FileName}: {Error}", fileName, ex.Message);
            }
        }

        public void ReplaceImage(string oldFile, string newFile, byte[] data)
        {
            DeleteImage(oldFile);
            SaveImage(newFile, data);
        }
    }
}
