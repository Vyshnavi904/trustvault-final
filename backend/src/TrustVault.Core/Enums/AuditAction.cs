namespace TrustVault.Core.Enums;

public enum AuditAction
{
    Login = 0,
    Logout = 1,
    Register = 2,
    DocumentUploaded = 3,
    DocumentDownloaded = 4,
    DocumentViewed = 5,
    DocumentEdited = 6,
    DocumentDeleted = 7,
    PermissionGranted = 8,
    PermissionRevoked = 9,
    AccessDenied = 10,
    UserDeactivated = 11,
    UserActivated = 12,
    UserRoleChanged = 13
}
