using VeltrexAPI.Infrastructure;

namespace VeltrexAPI.Services;

/// <summary>Saves uploaded QR images under wwwroot/uploads/qr and serves them as static files.</summary>
public class QrStorage(IWebHostEnvironment env)
{
    private const long MaxBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif"];
    private const string Folder = "uploads/qr";

    /// <summary>Stores the file and returns its path relative to wwwroot.</summary>
    public async Task<string> SaveAsync(IFormFile file, CancellationToken ct)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!file.ContentType.StartsWith("image/") || !AllowedExtensions.Contains(ext))
            throw ApiException.BadRequest("Please upload a PNG, JPG, WEBP or GIF image");
        if (file.Length > MaxBytes)
            throw ApiException.BadRequest("Image must be under 2 MB");

        var dir = Path.Combine(WebRoot, Folder);
        Directory.CreateDirectory(dir);

        // New name on every upload so browsers never show a cached old QR.
        var name = $"{Guid.NewGuid():N}{ext}";
        await using (var stream = File.Create(Path.Combine(dir, name)))
            await file.CopyToAsync(stream, ct);

        return $"{Folder}/{name}";
    }

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) || !relativePath.StartsWith(Folder + "/")) return;
        var full = Path.Combine(WebRoot, relativePath);
        if (File.Exists(full)) File.Delete(full);
    }

    private string WebRoot => env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
}
