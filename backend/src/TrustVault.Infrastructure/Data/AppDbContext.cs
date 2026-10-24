using Microsoft.EntityFrameworkCore;
using TrustVault.Core.Models;
using TrustVault.Core.Enums;

namespace TrustVault.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentPermission> DocumentPermissions => Set<DocumentPermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasOne(d => d.Owner)
                .WithMany(u => u.OwnedDocuments)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(d => !d.IsDeleted);
        });

        modelBuilder.Entity<DocumentPermission>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Permission).HasConversion<string>();

            e.HasOne(p => p.Document)
                .WithMany(d => d.Permissions)
                .HasForeignKey(p => p.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.User)
                .WithMany(u => u.ReceivedPermissions)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.GrantedBy)
                .WithMany(u => u.GrantedPermissions)
                .HasForeignKey(p => p.GrantedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).HasConversion<string>();
            e.HasIndex(a => a.Timestamp);
            e.HasIndex(a => a.DocumentId);
            e.HasIndex(a => a.UserId);

            e.HasOne(a => a.Document)
                .WithMany(d => d.AuditLogs)
                .HasForeignKey(a => a.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var adminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var ownerId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var viewerId = Guid.Parse("00000000-0000-0000-0000-000000000003");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminId,
                Email = "admin@trustvault.com",
                PasswordHash = "$2a$11$eECAnhYBOXXWTsuRc2Ckp.8wx.tqhmx3ET4SG2EOh7TviQS.LN5y2",
                FullName = "System Administrator",
                Role = UserRole.Administrator,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = ownerId,
                Email = "owner@trustvault.com",
                PasswordHash = "$2a$11$..YK2x1I63uSBZHSASgVM.EUckQ37Xu0vELDwCB0R4A/DAkzoNheK",
                FullName = "Document Owner",
                Role = UserRole.Owner,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = viewerId,
                Email = "viewer@trustvault.com",
                PasswordHash = "$2a$11$vzPK91FV6deEn/VzMrQSOuINoqMtmN2tYLimj5wRBGQOeuvuyX/Dm",
                FullName = "Document Viewer",
                Role = UserRole.Viewer,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
