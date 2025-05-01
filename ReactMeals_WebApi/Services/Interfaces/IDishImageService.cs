namespace ReactMeals_WebApi.Services.Interfaces
{
    // Interface that defines dish image operations
    public interface IDishImageService
    {
        public string ValidateImage(byte[] imageData);
        public void SaveImage(string fileName, byte[] data);
        public void ReplaceImage(string oldFile, string newFile, byte[] data);
    }
}
