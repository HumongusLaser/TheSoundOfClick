using Microsoft.AspNetCore.Components.Forms;

namespace TheSoundOfClick;

public class ProductImageStorage(IWebHostEnvironment environment)
{
    public const long MaxFileSize = 5 * 1024 * 1024;
    private const string ImageFolderName = "Image";

    public async Task<string> SaveAsync(IBrowserFile file)
    {
        if (file.Size is <= 0 or > MaxFileSize)
        {
            throw new InvalidDataException("Choose an image smaller than 5 MB.");
        }

        await using Stream input = file.OpenReadStream(MaxFileSize);
        await using var memoryStream = new MemoryStream();
        await input.CopyToAsync(memoryStream);
        byte[] imageBytes = memoryStream.ToArray();
        string extension = GetValidatedExtension(imageBytes);
        string fileName = $"{Guid.NewGuid():N}{extension}";
        string imageDirectory = Path.Combine(environment.ContentRootPath, ImageFolderName);
        Directory.CreateDirectory(imageDirectory);

        string filePath = Path.Combine(imageDirectory, fileName);
        try
        {
            await File.WriteAllBytesAsync(filePath, imageBytes);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            throw;
        }

        return $"/{ImageFolderName}/{fileName}";
    }

    public Task DeleteAsync(string imagePath)
    {
        string fileName = Path.GetFileName(imagePath);
        if (!string.Equals(imagePath, $"/{ImageFolderName}/{fileName}", StringComparison.Ordinal))
        {
            throw new ArgumentException("The product image path is invalid.", nameof(imagePath));
        }

        string filePath = Path.Combine(environment.ContentRootPath, ImageFolderName, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private static string GetValidatedExtension(byte[] imageBytes)
    {
        ReadOnlySpan<byte> bytes = imageBytes;
        if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }

        if (bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
        {
            return ".jpg";
        }

        if (bytes.Length >= 12 &&
            bytes[..4].SequenceEqual("RIFF"u8) &&
            bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        throw new InvalidDataException("Only PNG, JPEG, or WebP images are supported.");
    }
}
