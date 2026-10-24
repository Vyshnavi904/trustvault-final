using Microsoft.EntityFrameworkCore;
using TrustVault.Core.Enums;
using TrustVault.Core.Interfaces;
using TrustVault.Core.Models;
using TrustVault.Infrastructure.Data;
using TrustVault.Infrastructure.Services;
using Xunit;

namespace TrustVault.Tests;

public class AuthorizationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AuditService _auditService;
    private readonly PermissionService _permissionService;
    private readonly DocumentService _documentService;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _viewerId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public AuthorizationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _auditService = new AuditService(_db);
        _permissionService = new PermissionService(_db, _auditService);
        _documentService = new DocumentService(_db, new NoOpFileStorageService(),
            _permissionService, _auditService);

        SeedUsers();
    }

    private void SeedUsers()
    {
        _db.Users.AddRange(
            new User { Id = _adminId, Email = "admin@test.com", PasswordHash = "x", FullName = "Admin", Role = UserRole.Administrator },
            new User { Id = _ownerId, Email = "owner@test.com", PasswordHash = "x", FullName = "Owner", Role = UserRole.Owner },
            new User { Id = _viewerId, Email = "viewer@test.com", PasswordHash = "x", FullName = "Viewer", Role = UserRole.Viewer },
            new User { Id = _otherUserId, Email = "other@test.com", PasswordHash = "x", FullName = "Other", Role = UserRole.Viewer }
        );
        _db.SaveChanges();
    }

    private async Task<Document> CreateTestDocumentAsync()
    {
        var doc = new Document
        {
            Title = "Test Document",
            Description = "Test",
            Category = "Test",
            OriginalFileName = "test.txt",
            StoredPath = "test/path/test.txt",
            MimeType = "text/plain",
            FileSizeBytes = 100,
            OwnerId = _ownerId
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        return doc;
    }

    [Fact]
    public async Task Owner_CanDownload_OwnDocument()
    {
        var doc = await CreateTestDocumentAsync();
        var result = await _documentService.DownloadAsync(doc.Id, _ownerId, "127.0.0.1", "test");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UnauthorizedUser_CannotDownload_Document()
    {
        var doc = await CreateTestDocumentAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _documentService.DownloadAsync(doc.Id, _otherUserId, "127.0.0.1", "test"));
    }

    [Fact]
    public async Task UnauthorizedUser_CannotView_DocumentMetadata()
    {
        var doc = await CreateTestDocumentAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _documentService.GetByIdAsync(doc.Id, _otherUserId));
    }

    [Fact]
    public async Task UnauthorizedUser_CannotEdit_Document()
    {
        var doc = await CreateTestDocumentAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _documentService.UpdateAsync(new DocumentUpdateRequest(
                doc.Id, "New Title", "desc", "cat", _otherUserId)));
    }

    [Fact]
    public async Task UnauthorizedUser_CannotDelete_Document()
    {
        var doc = await CreateTestDocumentAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _documentService.DeleteAsync(doc.Id, _otherUserId));
    }

    [Fact]
    public async Task GrantedViewer_CanDownload_Document()
    {
        var doc = await CreateTestDocumentAsync();
        await _permissionService.GrantAsync(new GrantPermissionRequest(
            doc.Id, _viewerId, PermissionType.Read, _ownerId));

        var result = await _documentService.DownloadAsync(doc.Id, _viewerId, "127.0.0.1", "test");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GrantedViewer_CannotEdit_DocumentWithReadPermission()
    {
        var doc = await CreateTestDocumentAsync();
        await _permissionService.GrantAsync(new GrantPermissionRequest(
            doc.Id, _viewerId, PermissionType.Read, _ownerId));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _documentService.UpdateAsync(new DocumentUpdateRequest(
                doc.Id, "New Title", "desc", "cat", _viewerId)));
    }

    [Fact]
    public async Task AfterRevocation_ViewerCannotDownload_Document()
    {
        var doc = await CreateTestDocumentAsync();
        var perm = await _permissionService.GrantAsync(new GrantPermissionRequest(
            doc.Id, _viewerId, PermissionType.Read, _ownerId));

        await _permissionService.RevokeAsync(new RevokePermissionRequest(perm.Id, _ownerId));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _documentService.DownloadAsync(doc.Id, _viewerId, "127.0.0.1", "test"));
    }

    [Fact]
    public async Task AdminCanAccess_AnyDocument()
    {
        var doc = await CreateTestDocumentAsync();
        var result = await _documentService.DownloadAsync(doc.Id, _adminId, "127.0.0.1", "test");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task AccessDenied_IsLogged_InAuditTrail()
    {
        var doc = await CreateTestDocumentAsync();
        try
        {
            await _documentService.DownloadAsync(doc.Id, _otherUserId, "127.0.0.1", "test");
        }
        catch (UnauthorizedAccessException) { }

        var log = _db.AuditLogs.FirstOrDefault(a =>
            a.DocumentId == doc.Id && a.Action == AuditAction.AccessDenied);

        Assert.NotNull(log);
        Assert.False(log.IsSuccess);
        Assert.Equal(_otherUserId, log.UserId);
    }

    [Fact]
    public async Task Download_IsLogged_InAuditTrail()
    {
        var doc = await CreateTestDocumentAsync();
        await _documentService.DownloadAsync(doc.Id, _ownerId, "10.0.0.1", "test-agent");

        var log = _db.AuditLogs.FirstOrDefault(a =>
            a.DocumentId == doc.Id && a.Action == AuditAction.DocumentDownloaded);

        Assert.NotNull(log);
        Assert.True(log.IsSuccess);
        Assert.Equal("10.0.0.1", log.IpAddress);
    }

    [Fact]
    public async Task Viewer_CannotGrantPermissions()
    {
        var doc = await CreateTestDocumentAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _permissionService.GrantAsync(new GrantPermissionRequest(
                doc.Id, _otherUserId, PermissionType.Read, _viewerId)));
    }

    [Fact]
    public async Task Viewer_CannotUploadDocuments()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _documentService.UploadAsync(new DocumentUploadRequest(
                "Title", "Desc", "Cat",
                Stream.Null, "test.txt", "text/plain", 1, _viewerId)));
    }

    public void Dispose() => _db.Dispose();
}

public class NoOpFileStorageService : IFileStorageService
{
    public Task<string> UploadAsync(Stream s, string path, string ct) => Task.FromResult(path);
    public Task<Stream> DownloadAsync(string path) => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 }));
    public Task DeleteAsync(string path) => Task.CompletedTask;
}
