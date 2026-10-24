using Microsoft.Extensions.Configuration;
using TrustVault.Core.Interfaces;

namespace TrustVault.Infrastructure.Services;

public class SupabaseStorageService : IFileStorageService
{
    private readonly HttpClient _http;
    private readonly string _bucket;
    private readonly string _baseUrl;
    private readonly string _serviceKey;

    public SupabaseStorageService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _bucket = config["Supabase:Bucket"] ?? "documents";
        _baseUrl = config["Supabase:Url"]!.TrimEnd('/');
        _serviceKey = config["Supabase:ServiceKey"]!;

        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("apikey", _serviceKey);
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_serviceKey}");
    }

    public async Task<string> UploadAsync(Stream fileStream, string storedPath, string contentType)
    {
        var url = $"{_baseUrl}/storage/v1/object/{_bucket}/{storedPath}";

        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        var response = await _http.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        return storedPath;
    }

    public async Task<Stream> DownloadAsync(string storedPath)
    {
        var url = $"{_baseUrl}/storage/v1/object/{_bucket}/{storedPath}";
        var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    public async Task DeleteAsync(string storedPath)
    {
        var url = $"{_baseUrl}/storage/v1/object/{_bucket}/{storedPath}";
        var response = await _http.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
    }
}
