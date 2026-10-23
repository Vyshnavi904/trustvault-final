# PROMPTS.md — AI Usage Log

This file documents every significant AI interaction during development of TrustVault. Written as we went, not at the end.

---

## Entry 1 — 2026-10-21 — Architecture Design

**What I asked:** I described the full problem statement (TrustVault secure document platform) and asked the AI (Claude) to propose a complete architecture before writing any code. I asked it to take my suggestions (.NET, React, SQL) and plan the stack, roles, database schema, API design, security model, and deployment.

**What it produced:** A complete architecture plan with a tech stack table, three-role model, full database schema with five tables, API endpoint list, security architecture section, frontend page list, and development phase schedule.

**What was wrong with it:** It initially proposed SQL Server, which has no free cloud hosting. I corrected this to PostgreSQL. The plan was otherwise solid and used as the blueprint for the entire build.

**What I did instead:** Asked for PostgreSQL + Supabase. Confirmed the design then started development.

**Commit:** Initial project scaffold.

---

## Entry 2 — 2026-10-21 — Authorization Logic in PermissionService

**What I asked:** I asked the AI to write `HasPermissionAsync` — the core authorization check called before every document access, download, and edit.

**What it produced:**
```csharp
public async Task<bool> HasPermissionAsync(Guid documentId, Guid userId, PermissionType requiredPermission)
{
    var document = await _db.Documents.FindAsync(documentId);
    if (document == null || document.IsDeleted) return false;
    if (document.OwnerId == userId) return true;
    // ...
}
```

**What was wrong with it:** It used `_db.Documents.FindAsync()` which respects the EF Core query filter `d => !d.IsDeleted`. This means a deleted document would return `null`, and the method would return `false` — but not for the right reason. If the query filter changes, or if the document is found through a different path, the soft-delete check would be silently bypassed.

**What I did instead:** Changed to `_db.Documents.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == documentId)` and then added an explicit `document.IsDeleted` check. This way the security check is visible and not dependent on a query filter being present.

**Commit:** PermissionService: explicit soft-delete check in HasPermissionAsync

---

## Entry 3 — 2026-10-21 — Audit Log Write-Before-Serve Pattern

**What I asked:** I asked the AI to write the download endpoint that streams a file to the client.

**What it produced:** Standard code that checked permission, then fetched the file, then logged the download.

**What was wrong with it:** Logging after the file is served means that if the server crashes mid-stream, the download still happened but nothing is recorded. For an audit trail used as legal evidence, this is unacceptable — the log must exist even if the delivery fails.

**What I did instead:** Rewrote the download flow to write the `AuditLog.DocumentDownloaded` entry before calling `_storage.DownloadAsync()`. The audit entry is committed to the database first. If the file stream fails after that, the log still shows the access was granted and attempted.

**Commit:** DocumentService: write audit log before streaming file

---

## Entry 4 — 2026-10-21 — IFormFile in Core Layer

**What I asked:** I asked the AI to write the `IDocumentService` interface in the Core project (a classlib) using `IFormFile` as a parameter type.

**What it produced:**
```csharp
using Microsoft.AspNetCore.Http;
public record DocumentUploadRequest(string Title, ..., IFormFile File, Guid OwnerId);
```

**What was wrong with it:** `IFormFile` is in `Microsoft.AspNetCore.Http`, which is not available in a `classlib` project without adding an ASP.NET Core framework reference. This violates the layered architecture — the Core layer should have no dependency on any web framework. The build failed with CS0234.

**What I did instead:** Changed the interface to accept `Stream`, `string OriginalFileName`, `string ContentType`, and `long FileSize` instead of `IFormFile`. The controller layer (which does have access to ASP.NET Core) extracts these from `IFormFile` and passes them down. The Core layer stays framework-free.

**Commit:** Core: remove IFormFile from IDocumentService interface, use Stream

---

## Entry 5 — 2026-10-21 — Access Denied Returns 403, Not 404

**What I asked:** I asked the AI to write the document metadata endpoint (`GET /api/documents/{id}`).

**What it produced:** Code that returned `404 Not Found` when a user did not have permission to view a document.

**What was wrong with it:** Returning 404 leaks information — it tells an attacker whether a document exists at that ID. If they get 404, they learn the document does not exist. If they get 403, they learn it does but they cannot access it. While 403 also leaks existence, returning 404 is even worse because it lets an attacker enumerate existing IDs by noting which ones switch from 404 to 403 after they gain any access. More importantly, for access-control systems it is important that the distinction between "does not exist" and "you cannot see it" is explicit in the code.

**What I did instead:** Changed all authorization failures to throw `UnauthorizedAccessException`, which the error middleware maps to `403 Forbidden`. The audit log records the denied attempt with the requesting user's ID. This is consistent with the principle that authorization decisions should be explicit, logged, and unambiguous.

**Commit:** API: map UnauthorizedAccessException to 403, log all AccessDenied events

---

## Context Files Reused

The following prompt context was reused across the session:

```
You are building TrustVault: a secure document sharing platform.
Security rules that are non-negotiable:
1. Server decides authorization on every request — no client-side gates.
2. Files are never served from direct storage URLs.
3. Audit log is append-only — no UPDATE or DELETE on AuditLogs table.
4. Permission revocation must be immediate — re-check DB on every request.
5. AccessDenied events must always be logged, including the requesting user ID.
```

This context was included at the start of every code-generation prompt for anything touching authorization, downloads, or the audit trail.
