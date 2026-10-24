namespace TrustVault.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string storedPath, string contentType);
    Task<Stream> DownloadAsync(string storedPath);
    Task DeleteAsync(string storedPath);
}
