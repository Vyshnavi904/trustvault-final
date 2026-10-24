# TrustVault — Complete Deployment Guide

**Read this entire guide once before starting. It takes about 30–45 minutes to complete.**
**You do not need any coding knowledge. Just follow each step exactly.**

---

> **DO YOU NEED TO INSTALL ANYTHING ON YOUR COMPUTER?**
> **No.** You do not need to install Node.js, npm, .NET, Docker, Git, or any other software.
> - Netlify automatically installs npm packages and builds the website **in their cloud**.
> - Render automatically builds the Docker container **in their cloud**.
> - You only need a web browser and the zip file you received.

---

## What You Will Set Up

| Service | What it does | Cost |
|---------|-------------|------|
| GitHub | Stores and hosts your code | Free |
| Supabase | Database + file storage | Free |
| Render | Runs the backend server | Free |
| Netlify | Hosts the website (frontend) | Free |

At the end you will have two URLs:
- A **live website URL** (from Netlify) — this is the one you share
- A **backend URL** (from Render) — used internally by the website

---

## Accounts to Create First

Before doing anything else, create free accounts on these four websites.  
**Use the same email address for all four — it makes things easier.**

1. [github.com](https://github.com) → click **Sign up**
2. [supabase.com](https://supabase.com) → click **Start your project**
3. [render.com](https://render.com) → click **Get Started for Free**
4. [netlify.com](https://netlify.com) → click **Sign up**

When creating the Render and Netlify accounts, choose **"Sign up with GitHub"** so they connect automatically.

---

## STEP 1 — Upload the Code to GitHub

### 1.1 — Create a new repository on GitHub

1. Log in to **github.com**
2. Click the **+** button in the top-right corner → **New repository**
3. Fill in:
   - **Repository name:** `trustvault`
   - **Visibility:** Select **Private** ← important, keep it private until submission deadline
   - Leave everything else as default
4. Click **Create repository**

### 1.2 — Upload the project files

You will now upload everything from the zip file you received.

1. On the new repository page, click **uploading an existing file**
   *(It appears as a link in the middle of the screen)*

2. Open the zip file you received and extract it. You will see a folder called `Project-V` (or similar). Open that folder — you should see:
   ```
   backend/
   frontend/
   THREATS.md
   PROMPTS.md
   README.md
   DEPLOYMENT_GUIDE.md
   .gitignore
   ```

3. Select ALL files and folders inside that folder (Ctrl+A on Windows, Cmd+A on Mac)

4. Drag and drop them into the GitHub upload area in your browser

5. Wait for the upload to finish (may take 1–2 minutes for the backend)

6. In the **Commit changes** box at the bottom, type: `Initial project upload`

7. Click **Commit changes**

**You should now see all the files listed on the GitHub repository page.**

---

## STEP 2 — Set Up Supabase (Database + File Storage)

### 2.1 — Create a Supabase project

1. Log in to **supabase.com**
2. Click **New project**
3. Fill in:
   - **Name:** `trustvault`
   - **Database Password:** Make up a strong password and **write it down** — you need it later
   - **Region:** Choose the one closest to you
4. Click **Create new project** and wait 1–2 minutes for it to set up

### 2.2 — Create the file storage bucket

1. In the left sidebar, click **Storage**
2. Click **New bucket**
3. Fill in:
   - **Name:** `documents` ← must be exactly this
   - Toggle **Public bucket** to **OFF** (it must be private)
4. Click **Save**

### 2.3 — Collect your Supabase credentials (you need 4 things)

**Thing 1 & 2 — Project URL and Service Role Key:**

1. In the left sidebar, click **Project Settings** (gear icon at the bottom)
2. Click **API** in the submenu
3. Find and copy these two values — paste them into Notepad/TextEdit to save them:
   - **Project URL** — looks like `https://abcdefghij.supabase.co`
   - **service_role** secret key — a long string starting with `eyJ...` ← use the **service_role** key, NOT the **anon** key

**Thing 3 — Your project reference ID:**

1. Look at your Project URL from Thing 1 — it looks like `https://abcdefghij.supabase.co`
2. The part between `https://` and `.supabase.co` is your **project reference** (e.g. `abcdefghij`)
3. Note it down — you will use it to build the connection string in Step 3.3

**Thing 4 — Bucket name:** This is just `documents` (the one you created)

---

## STEP 3 — Deploy the Backend to Render

### 3.1 — Create a new Web Service on Render

1. Log in to **render.com**
2. Click **New +** in the top navigation → **Web Service**
3. Select **Build and deploy from a Git repository**
4. Connect your GitHub account if prompted, then find and select your `trustvault` repository
5. Click **Connect**

### 3.2 — Configure the Web Service

Fill in these settings on the next page:

| Setting | Value |
|---------|-------|
| **Name** | `trustvault-api` |
| **Region** | Choose same region as your Supabase project |
| **Branch** | `main` |
| **Root Directory** | `backend` ← type this exactly |
| **Runtime** | `Docker` |
| **Instance Type** | `Free` |

Leave everything else as default.

### 3.3 — Add Environment Variables

Scroll down to the **Environment Variables** section. Click **Add Environment Variable** for each row below.  
**Copy-paste the names exactly — they are case-sensitive.**

| Key | Value |
|-----|-------|
| `ConnectionStrings__DefaultConnection` | Fill in this template (replace the two ALL-CAPS words):<br>`Host=db.YOURPROJECTREF.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOURPASSWORD;SSL Mode=Require;Trust Server Certificate=true`<br>Example: `Host=db.abcdefghij.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=MyPass123!;SSL Mode=Require;Trust Server Certificate=true` |
| `Jwt__Secret` | Type any long random string of at least 32 characters, e.g. `TrustVault2024SecureKey!XYZ987Random` |
| `Jwt__Issuer` | `TrustVault` |
| `Jwt__Audience` | `TrustVaultUsers` |
| `Supabase__Url` | Your Supabase Project URL from Step 2.3, e.g. `https://abcdefghij.supabase.co` |
| `Supabase__ServiceKey` | Your service_role key from Step 2.3 |
| `Supabase__Bucket` | `documents` |
| `Cors__AllowedOrigins` | `https://placeholder.netlify.app` ← temporary, you will update this in Step 5 |

> **Tip:** `YOURPROJECTREF` is the value you noted in Step 2.3 Thing 3 — the string between `https://` and `.supabase.co` in your Supabase URL. Do not include spaces anywhere in the connection string.

### 3.4 — Deploy

1. Scroll to the bottom and click **Create Web Service**
2. Render will now build your backend. This takes **5–10 minutes** on the first deploy.
3. You will see build logs scrolling. Wait until you see a green **Live** status badge.
4. If the build fails, see the Troubleshooting section at the end of this guide.

### 3.5 — Copy your Backend URL

Once deployed, you will see your service URL at the top of the Render page.  
It looks like: `https://trustvault-api.onrender.com`  
**Copy and save this URL — you need it in Step 4.**

### 3.6 — Verify the backend is working

Open a new browser tab and go to: `https://YOUR-RENDER-URL/swagger`  
You should see a page titled "TrustVault API" with a list of endpoints.  
If you see this page, the backend is working correctly.

---

## STEP 4 — Deploy the Frontend to Netlify

> **You do not need to install Node.js or npm.** Netlify runs `npm install` and `npm run build` automatically on their servers. The project already includes a `netlify.toml` configuration file that tells Netlify exactly what to do — you do not need to manually set the build command or publish directory.

### 4.1 — Create a new site on Netlify

1. Log in to **netlify.com**
2. Click **Add new site** → **Import an existing project**
3. Click **Deploy with GitHub**
4. Find and select your `trustvault` repository
5. Netlify will show a **"Configure site and deploy"** screen — **DO NOT click Deploy yet**

### 4.2 — Add the Environment Variable BEFORE deploying

> **This step is critical.** The `VITE_API_URL` value gets baked into the website during the build. If you deploy without setting it, the website will not be able to reach the backend. You must set it **before** clicking Deploy.

1. On the "Configure site and deploy" screen, scroll down to **Environment variables**
2. Click **Add a variable**
3. Add exactly:
   - **Key:** `VITE_API_URL`
   - **Value:** Your Render backend URL from Step 3.5, e.g. `https://trustvault-api.onrender.com`
   - Make sure there is **no trailing slash** at the end of the URL
4. Click **Save**

### 4.3 — Deploy

1. Now click **Deploy trustvault** (or **Deploy site**)
2. Netlify will automatically:
   - Download your code from GitHub
   - Run `npm install` (installs all packages in the cloud — you do not need npm installed)
   - Run `npm run build` (builds the website in the cloud)
3. This takes **2–3 minutes**. Wait for the green **Published** status.
4. Copy your Netlify URL — it looks like: `https://trustvault-abc123.netlify.app`

> **If you accidentally deployed without setting VITE_API_URL:** Go to Site Settings → Environment variables → Add the variable → Then go to Deploys tab → click **Trigger deploy** → **Deploy site** to rebuild with the correct URL.

---

## STEP 5 — Update CORS Setting on Render

Now that you have your Netlify URL, you need to update the backend so it accepts requests from it.

1. Go back to **render.com** and open your `trustvault-api` service
2. Click **Environment** in the left sidebar
3. Find the `Cors__AllowedOrigins` variable and click **Edit**
4. Replace the placeholder value with your Netlify URL:
   - Value: `https://trustvault-abc123.netlify.app` ← use your actual Netlify URL
5. Click **Save Changes**
6. Render will automatically redeploy. Wait for the **Live** green status to return (1–2 minutes).

---

## STEP 6 — Test the Deployment

### 6.1 — Test in a private/incognito window

1. Open a **private/incognito browser window** (important — tests fresh login)
2. Go to your Netlify URL
3. You should see the TrustVault login page

> **First load note:** The Render free tier "sleeps" after 15 minutes of no traffic. The first visit after sleeping takes **30–60 seconds** to wake up. This is normal. Subsequent visits are instant.

### 6.2 — Test with the demo accounts

Test all three roles in order:

**Test 1 — Admin login:**
- Email: `admin@trustvault.com`
- Password: `Admin@123`
- You should see the Documents page with Users and Audit links in the navigation

**Test 2 — Owner login (new tab):**
- Email: `owner@trustvault.com`
- Password: `Owner@123`
- Click **Upload Document**, upload any PDF or text file
- Verify the document appears in the list

**Test 3 — Viewer trying to access Owner's document:**
- In another incognito window, log in as `viewer@trustvault.com` / `Viewer@123`
- You should see no documents (the list is empty)
- If you try to go directly to the document URL, you should get an error (access denied)

**Test 4 — Grant access and verify it works:**
- As the Owner, open the uploaded document and click the Share icon
- Type `viewer@trustvault.com` and click Grant Access
- As the Viewer, refresh the documents page — the document should now appear
- Download it — it should work

**Test 5 — Revoke access:**
- As the Owner, open the document → Permissions panel → click X next to the Viewer
- As the Viewer, try to download again — it should fail immediately

**Test 6 — Check audit log:**
- Log in as Admin → click Audit in the navigation
- You should see every action recorded: uploads, downloads, denials, permission changes

### 6.3 — All tests pass = deployment is complete ✓

---

## STEP 7 — Register Your Own Account

To use the system with your own account:

1. Go to your Netlify URL
2. Click **Register**
3. Fill in your details and choose **Owner** role
4. You can now upload and share documents

---

## Troubleshooting

### "Build failed" on Render

1. Click **Logs** in the Render dashboard to see the error
2. Most common cause: wrong connection string format
3. Check that you used the semicolon-separated format (not the URI format) for `ConnectionStrings__DefaultConnection`
4. Check there are no extra spaces in any environment variable

### "Application error" or blank page on Netlify

1. Go to Netlify → your site → **Deploys** tab
2. Click the latest deploy and check the logs
3. Most common cause: wrong `VITE_API_URL` (missing `https://` or trailing slash)
4. If you change an environment variable, click **Trigger deploy** → **Deploy site** to rebuild

### "Access denied" or 403 errors in the app

1. Check that `Cors__AllowedOrigins` on Render exactly matches your Netlify URL (including `https://`)
2. Check that you waited for Render to finish redeploying after changing the CORS setting

### The site loads but login fails

1. Open browser developer tools (press F12) → Console tab
2. Look for any red error messages
3. Most common: the backend URL is wrong in Netlify's `VITE_API_URL` — make sure it matches the Render URL exactly

### First login is very slow (30–60 seconds)

This is normal. Render's free tier sleeps the server after 15 minutes of inactivity. The first request wakes it up. Subsequent requests are fast.

---

## Your Credentials Summary

Fill this in as you go through the steps:

| Item | Your Value |
|------|-----------|
| GitHub repository URL | https://github.com/YOUR-USERNAME/trustvault |
| Supabase Project URL | https://_____________.supabase.co |
| Render backend URL | https://trustvault-api.onrender.com |
| Netlify frontend URL | https://_____________.netlify.app |

---

## Default Accounts in the System

| Role | Email | Password |
|------|-------|----------|
| Administrator | admin@trustvault.com | Admin@123 |
| Document Owner | owner@trustvault.com | Owner@123 |
| Viewer | viewer@trustvault.com | Viewer@123 |

These accounts are created automatically when the backend starts for the first time. You can change these passwords from the Admin panel after logging in.

---

## What Each Role Can Do

| Action | Administrator | Owner | Viewer |
|--------|:---:|:---:|:---:|
| Upload documents | ✓ | ✓ | ✗ |
| View own documents | ✓ | ✓ | ✗ |
| View shared documents | ✓ | ✓ | ✓ |
| Download documents | ✓ | ✓ | If shared |
| Edit documents | ✓ | Own only | If granted Edit |
| Delete documents | ✓ | Own only | ✗ |
| Grant/revoke access | ✓ | Own docs | ✗ |
| View audit log | All logs | Own docs | ✗ |
| Manage users | ✓ | ✗ | ✗ |
