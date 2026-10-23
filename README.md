# TrustVault — Secure Document Sharing Platform

A document platform where the wrong person cannot reach a document, and where you can prove exactly who reached it.

---

## Demo Credentials

| Role | Email | Password |
|------|-------|----------|
| Administrator | admin@trustvault.com | Admin@123 |
| Document Owner | owner@trustvault.com | Owner@123 |
| Viewer | viewer@trustvault.com | Viewer@123 |

---

## What It Does

- **Three roles**: Administrator, Owner, Viewer — each with clearly defined permissions
- **Document management**: upload, search, update, soft-delete documents (PDF, Word, Excel, images, ZIP, up to 50 MB)
- **Granular permissions**: owners grant Read or Edit access to named users, and revoke it immediately
- **Full audit trail**: every upload, download, permission change, and denied access is logged with user, timestamp, and IP — the log is append-only
- **Administrative views**: user management (activate/deactivate, change role), system-wide audit log

---

## Architecture

```
frontend/          React 18 + TypeScript + Vite + Tailwind CSS → Netlify
backend/
  src/
    TrustVault.API/             ASP.NET Core 10 Web API → Render.com
    TrustVault.Core/            Domain models, interfaces (no framework deps)
    TrustVault.Infrastructure/  EF Core, PostgreSQL, Supabase Storage
  tests/
    TrustVault.Tests/           xUnit authorization tests
```

**External services (all free tier):**
- Database: Supabase PostgreSQL
- File storage: Supabase Storage (private bucket)
- Backend: Render.com (Docker container)
- Frontend: Netlify

---

## Permission Model

Permissions are per-document, per-user, with two levels: Read (view + download) and Edit (also update metadata/file).

- The document **Owner** has full control of their own documents
- The **Administrator** has full system-wide access and cannot be blocked
- A **Viewer** can only see documents explicitly shared with them
- Taking access away works immediately — the next request is refused

The server re-checks permissions on every request against the live database. No permission decision is ever cached.

---

## Security Design

The three non-negotiable rules implemented:

1. **Files are never reachable without authorization**: Supabase bucket is private. All downloads go through `GET /api/documents/{id}/download` which checks permissions before fetching the file server-side.

2. **Audit trail is tamper-resistant**: No API endpoint exists to modify or delete audit logs. The `AuditLog` table uses INSERT-only access in the application layer.

3. **Revocation is immediate**: Permission checks query the live database. Setting `IsActive = false` on a permission takes effect on the next request.

See [THREATS.md](THREATS.md) for the full threat model.

---

## Deployment Instructions

### Step 1 — Set up Supabase (free at supabase.com)

1. Create a new Supabase project
2. In **Storage**, create a bucket named `documents` and set it to **Private**
3. In **Project Settings → API**, copy:
   - **Project URL** → this is your `Supabase:Url`
   - **service_role** key → this is your `Supabase:ServiceKey`
4. In **Project Settings → Database**, copy the connection string (Host, Password)

### Step 2 — Deploy Backend to Render.com (free)

1. Push the `backend/` folder to your GitHub repository
2. Go to Render.com → New → Web Service
3. Connect your GitHub repo, select the `backend/` directory
4. Set runtime to **Docker** (Render detects the Dockerfile automatically)
5. Add these environment variables in Render dashboard:

```
ConnectionStrings__DefaultConnection = Host=db.YOURPROJECT.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOURPASSWORD;SSL Mode=Require;Trust Server Certificate=true

Jwt__Secret = (generate a random 64-character string)
Jwt__Issuer = TrustVault
Jwt__Audience = TrustVaultUsers

Supabase__Url = https://YOURPROJECT.supabase.co
Supabase__ServiceKey = your-service-role-key
Supabase__Bucket = documents

Cors__AllowedOrigins = https://your-netlify-app.netlify.app
```

6. Deploy. On first start, EF Core runs migrations and seeds the three demo users automatically.
7. Copy the Render service URL (e.g., `https://trustvault.onrender.com`)

### Step 3 — Deploy Frontend to Netlify (free)

1. In the `frontend/` directory, create a file named `.env` with:
```
VITE_API_URL=https://trustvault.onrender.com
```

2. Commit and push to GitHub
3. In Netlify → Add new site → Import from Git
4. Set **Build command**: `npm run build`
5. Set **Publish directory**: `dist`
6. Add environment variable `VITE_API_URL = https://trustvault.onrender.com`
7. The `frontend/netlify.toml` file handles SPA routing automatically

### Step 4 — Verify

1. Open your Netlify URL in an incognito window
2. Log in as `admin@trustvault.com` / `Admin@123`
3. Log in as `owner@trustvault.com` in another window, upload a document
4. Try to download it as `viewer@trustvault.com` — it should fail (403)
5. Grant the viewer access from the owner account — download now works
6. Revoke access — download fails again immediately
7. Check the Admin → Audit log to see every step recorded

---

## Local Development

### Backend

```bash
# Prerequisites: .NET 10 SDK, PostgreSQL running locally

cd backend

# Set up local database connection in appsettings.Development.json
# (defaults to Host=localhost;Database=trustvault)

dotnet run --project src/TrustVault.API
# API available at http://localhost:5260
# Swagger UI at http://localhost:5260/swagger
```

### Run Tests

```bash
cd backend
dotnet test tests/TrustVault.Tests
# Expected: 13 passed, 0 failed
```

### Frontend

```bash
cd frontend
npm install
cp .env.example .env   # edit VITE_API_URL if needed
npm run dev
# UI at http://localhost:5173
```

---

## What I Chose Not to Build

- **External sharing**: No sharing with users outside the system. All users must register. This reduces attack surface significantly — there are no anonymous or link-based access flows to secure.
- **File versioning**: Documents are replaced on update, not versioned. Audit trail records updates. This is a deliberate simplification.
- **Folder hierarchy**: Documents are flat with category tags. Folder inheritance adds permission complexity (do child folders inherit parent permissions?) that introduces subtle bugs.
- **AI-assisted permissions**: The problem statement mentioned suggesting who should see a document. Skipped — it adds no security value and no OpenAI key was available.
- **Retention / scheduled deletion**: Out of scope for this build.

A smaller system with a permission model that holds beats a larger one with a hole in it.

---

## API Documentation

All endpoints require `Authorization: Bearer {token}` except `/api/auth/register` and `/api/auth/login`.

| Method | Endpoint | Who can call |
|--------|----------|-------------|
| POST | /api/auth/register | Public |
| POST | /api/auth/login | Public |
| POST | /api/auth/refresh | Public |
| POST | /api/auth/logout | Authenticated |
| GET | /api/auth/me | Authenticated |
| GET | /api/documents | Authenticated (sees only accessible docs) |
| POST | /api/documents | Owner, Administrator |
| GET | /api/documents/{id} | Users with Read permission or Owner/Admin |
| PUT | /api/documents/{id} | Users with Edit permission or Owner/Admin |
| DELETE | /api/documents/{id} | Owner, Administrator |
| GET | /api/documents/{id}/download | Users with Read permission or Owner/Admin |
| GET | /api/documents/{id}/permissions | Owner, Administrator |
| POST | /api/documents/{id}/permissions | Owner, Administrator |
| DELETE | /api/documents/{id}/permissions/{permId} | Owner, Administrator |
| GET | /api/audit | Administrator (all), Owner (own docs only) |
| GET | /api/admin/users | Administrator |
| PATCH | /api/admin/users/{id}/active | Administrator |
| PATCH | /api/admin/users/{id}/role | Administrator |
| GET | /api/admin/documents | Administrator |

---

## Running from a Clean Clone

```bash
git clone https://github.com/your-username/trustvault
cd trustvault

# Backend
cd backend
dotnet restore
dotnet build

# Tests (requires no database — uses in-memory)
dotnet test tests/TrustVault.Tests

# Frontend
cd ../frontend
npm install
npm run build
```

The backend requires a live PostgreSQL database for `dotnet run`. The tests do not — they use EF Core InMemory.
