# JobtrackerBackend 📋

ASP.NET Core 9 Web API backend for a job tracking system, designed to work with a .NET MAUI frontend.  
This backend connects to Supabase for authentication, storage (buckets), and PostgreSQL database access.

---

## ✨ Features

- JWT Bearer authentication validated against Supabase  
- CRUD endpoints for managing job applications  
- Supports file uploads (CV and application files) to Supabase Storage buckets  
- Data models and DTOs organized in dedicated folder  
- Ready for Docker containerization and cloud hosting (planned on Render)  

---

## 🛠️ Tech Stack

- ASP.NET Core 9 Web API  
- Supabase .NET SDK (`Supabase` v1.1.1)  
- JWT Bearer & OpenID Connect authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.Authentication.OpenIdConnect`)  
- Entity Framework Core with `Npgsql` for PostgreSQL  
- Microsoft.Identity.Web packages for downstream API integration  
- Docker for containerization  

---

## 📂 Project Structure
├── Models/                # Data models & DTO classes
├── Enums/                 # Application status enums
├── Program.cs             # Endpoint routing and app setup (includes Supabase client and JWT auth)
├── Dockerfile             # Docker container setup
├── compose.yaml           # Docker compose (if applicable)
├── appsettings.Development.json  # Local config including Supabase keys (secret)

---

## 🚀 Getting Started

### 1. Clone the repo

```bash
git clone https://github.com/woidthevoid/JobtrackerBackend.git
cd JobtrackerBackend
```
2. Configure Supabase keys

Modify appsettings.Development.json with your Supabase credentials:
{
  "Supabase": {
    "Url": "https://your-supabase-url.supabase.co",
    "Key": "your-anon-or-service-role-key",
    "ServiceRoleKey": "your-service-role-secret-key"
  }
}

3. Restore dependencies
   ```bash
   dotnet restore
   ```

4. Run the API locally
   ```bash
   dotnet run
   ```
##Authentication
	•	The API expects a JWT Bearer token from the frontend in the Authorization header:
  Authorization: Bearer <token>
  •	Tokens are validated against Supabase’s JWT issuer (supabase) and audience (authenticated).
	•	The ServiceRoleKey is used as the signing key for token validation.
	•	Unauthorized or forbidden requests receive HTTP 401 or 403 responses respectively.

## API Endpoints

🔹 GET /jobs
Description:
Get all job applications for the authenticated user.
Request:
	•	No body needed
	•	Must include a valid bearer token
Response:
200 OK with a list of job applications (see example below)

🔹 POST /jobs
Description:
Create a new job application with optional file uploads.
Request:
multipart/form-data
Response:
	•	201 Created with the created job DTO
	•	400 Bad Request if fields are missing
	•	401 Unauthorized if token is invalid

🔹 DELETE /jobs/{id}
Description:
Delete a job application (if owned by the user).
Request:
	•	{id} = job application ID (GUID)
	•	Must include bearer token
Response:
	•	200 OK if deleted
	•	404 Not Found if not found or doesn’t belong to user
	•	403 Forbidden if unauthorized

## 🐳 Docker & Deployment
	•	The project includes a Dockerfile and optional compose.yaml for containerization.
	•	Plan to deploy on Render or similar cloud platforms.
	•	Configure environment variables or secret managers to provide Supabase credentials and JWT secrets securely in production.
