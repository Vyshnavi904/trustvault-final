# THREATS.md — TrustVault Threat Model

This document describes every attack vector we considered, what we did about each, and which ones we knowingly accepted and why.

---

## T1 — Direct Storage URL Access

**Threat:** A user guesses or finds the file's storage path and accesses it directly from Supabase Storage, bypassing the API entirely.

**What we did:** The Supabase Storage bucket is set to **private**. No public access policy is enabled. Every file download goes through `GET /api/documents/{id}/download`, which checks permissions server-side before fetching the file from Supabase using the service role key and streaming it to the client. The stored path is a UUID-prefixed path (`{ownerId}/{randomGuid}/{filename}`) and is never exposed to the client in any response.

**Test:** There is no public URL for any file. The bucket URL with a valid file path returns 400/403 without the service role key.

**Status: CLOSED**

---

## T2 — Broken Object Level Authorization (BOLA) on Download

**Threat:** An authenticated user changes the document UUID in `GET /api/documents/{id}/download` to a document they do not own or have access to.

**What we did:** The download endpoint calls `PermissionService.HasPermissionAsync()` before streaming. This method queries the live database on every request — no cached results. It checks:
- Is the user the document owner? → Allow
- Is the user an Administrator? → Allow
- Does an active `DocumentPermission` row exist for this user + document? → Allow
- Otherwise → 403 and log an `AccessDenied` audit entry

**Test:** `AuthorizationTests.UnauthorizedUser_CannotDownload_Document` covers this.

**Status: CLOSED**

---

## T3 — Access After Revocation

**Threat:** A user was granted access, we revoke it, but they still have a session or cached token that lets them download.

**What we did:** Permission revocation sets `DocumentPermissions.IsActive = false` and stamps `RevokedAt`. Every download request re-queries this table. There is no caching of permission decisions. Revocation is effective on the next request — within milliseconds.

**Accepted risk:** If a download is in-flight when revocation happens, that single download completes. This is unavoidable for streaming responses.

**Test:** `AuthorizationTests.AfterRevocation_ViewerCannotDownload_Document` covers this.

**Status: CLOSED (one in-flight request accepted)**

---

## T4 — JWT Token Theft and Replay

**Threat:** An attacker steals an access token from localStorage and uses it to access documents.

**What we did:**
- Access tokens expire in **15 minutes**
- Refresh tokens expire in **7 days** and are stored hashed in the database; the raw token is in localStorage only on the client
- Logout immediately invalidates the refresh token server-side
- All communication is over HTTPS (enforced on Render and Netlify)

**Accepted risk:** If localStorage is compromised (e.g., via XSS), the attacker has up to 15 minutes of access. Mitigating XSS fully requires a stricter CSP and HttpOnly cookies for tokens — this is the next improvement.

**Improvement path:** Move tokens to HttpOnly cookies with SameSite=Strict for production hardening.

**Status: MITIGATED — 15 min window accepted**

---

## T5 — Audit Log Tampering

**Threat:** An attacker (or a compromised admin) modifies or deletes audit log entries to hide their tracks.

**What we did:**
- There is **no API endpoint** for updating or deleting `AuditLog` records
- The `AuditLog` table has no `DELETE` or `UPDATE` operations in any service — only `INSERT`
- Entries are written by the server, not the client — timestamps, IP, and action type are all server-assigned
- The database application user should have INSERT-only permission on `audit_logs` in production

**Accepted risk:** A DBA with direct database access could still tamper. Full immutability would require a write-once storage or cryptographic chaining — out of scope for this build, but documented in THREATS.md as the next hardening step.

**Status: MITIGATED — DBA access accepted**

---

## T6 — Mass Assignment / Role Escalation via Registration

**Threat:** A user registers with a crafted payload setting `role: "Administrator"` to elevate themselves.

**What we did:** The `AuthService.RegisterAsync` method only accepts `"Owner"` or `"Viewer"` roles. Any other value (including `"Administrator"`) defaults to `Viewer`. Role is set server-side, never trusted from client input.

**Test:** Tested manually. Registering with `role: "Administrator"` results in `Viewer` role.

**Status: CLOSED**

---

## T7 — Enumeration of Document IDs

**Threat:** An attacker systematically tries UUIDs to enumerate documents and extract titles or metadata.

**What we did:** All document endpoints (`GET /documents/{id}`, `GET /documents/{id}/download`) require authentication and check permissions. An unauthenticated request gets 401. An authenticated user without access gets 403. We return 403 (not 404) to avoid revealing whether a document exists at all.

**Accepted risk:** An authenticated user with valid credentials can still enumerate IDs and learn which UUIDs do not return 403 (i.e., which ones they have access to). This is acceptable — they are supposed to know about their own documents.

**Status: CLOSED for unauthenticated; ACCEPTED for authenticated enumeration**

---

## T8 — File Type Bypass (MIME Sniffing / Polyglot Files)

**Threat:** An attacker uploads a malicious file disguised as a PDF (e.g., a PDF with embedded JavaScript, or an HTML file with a `.pdf` extension).

**What we did:**
- Validate the `Content-Type` header sent by the client against an allowlist
- Validate file size (max 50 MB)
- Files are stored and served as-is; we do not execute them

**Accepted risk:** We trust the MIME type from the HTTP headers, not the file magic bytes. A client could spoof a MIME type. Full mitigation requires reading the first few bytes of the file (magic number check). We explicitly accept this risk because the file is only served to authorized users who chose to download it — the attack surface is limited. Adding libmagic or similar is documented as a next step.

**Status: MITIGATED — magic byte check accepted as future work**

---

## T9 — Insecure Direct Object Reference on Permissions

**Threat:** A user calls `DELETE /api/documents/{docId}/permissions/{permId}` with someone else's permission ID to revoke access on a document they do not own.

**What we did:** `PermissionService.RevokeAsync` loads the permission, then loads its associated document, and checks that the requesting user is the document owner (or Administrator). If not, it throws 403.

**Test:** `AuthorizationTests.Viewer_CannotGrantPermissions` covers part of this.

**Status: CLOSED**

---

## T10 — Soft Delete Bypass

**Threat:** A user who knew a document's UUID tries to download it after it has been soft-deleted.

**What we did:** The `HasPermissionAsync` check loads the document with `IgnoreQueryFilters()` and explicitly checks `document.IsDeleted`. If deleted, it returns `false` regardless of permission records.

**Status: CLOSED**

---

## T11 — Deactivated User Still Accessing Documents

**Threat:** An admin deactivates a user, but they still have a valid access token (up to 15 minutes) and use it to download documents.

**What we did:** `HasPermissionAsync` fetches the `User` entity and checks `user.IsActive`. A deactivated user's JWT still validates cryptographically, but any permission check will return false because `IsActive = false`.

**Status: CLOSED**

---

## T12 — Cross-Origin Requests from Unauthorized Domains

**Threat:** A malicious website makes requests to the API using a victim's browser cookies/tokens.

**What we did:** CORS policy is configured with explicit allowed origins (`AllowedOrigins` from config). Only the frontend's domain is whitelisted. Preflight requests from other origins are rejected.

**Status: CLOSED**

---

## Summary Table

| ID | Threat | Status |
|---|---|---|
| T1 | Direct storage URL access | CLOSED |
| T2 | BOLA on download | CLOSED |
| T3 | Access after revocation | CLOSED (one in-flight request accepted) |
| T4 | JWT theft | MITIGATED (15 min window) |
| T5 | Audit log tampering | MITIGATED (DBA accepted) |
| T6 | Role escalation via registration | CLOSED |
| T7 | Document ID enumeration | CLOSED / ACCEPTED |
| T8 | File type bypass | MITIGATED (magic bytes future work) |
| T9 | IDOR on permission revocation | CLOSED |
| T10 | Soft delete bypass | CLOSED |
| T11 | Deactivated user token replay | CLOSED |
| T12 | Cross-origin requests | CLOSED |
