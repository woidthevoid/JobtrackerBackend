# JobtrackerBackend

Backend for a job tracker application, designed to keep track of your job applications and their states in the hiring process.  
This backend connects your .NET MAUI frontend to a Supabase instance for data storage and authentication.  
Authentication is handled via JWT Bearer tokens.

---

## ✨ Features

- JWT Bearer authentication validated against Supabase.
- CRUD endpoints for managing job applications.
- Supports file uploads (CV, application files) to Supabase Storage buckets.
- Organized data models and DTOs.
- Ready for local Docker or .NET CLI development.

---

## 🛠️ Tech Stack

- ASP.NET Core 9 Web API
- Supabase .NET SDK
- JWT Bearer Authentication
- Entity Framework Core & PostgreSQL (via Supabase)
- Docker for containerization

---

## 📂 Project Structure
├── Models/ # Data models & DTO classes	

├── Program.cs # Endpoint routing, Supabase client, JWT auth

├── Dockerfile # Docker image build instructions

├── compose.yaml # Docker Compose file (optional)

├── appsettings.Development.json # Local config including Supabase keys


---

## 🚀 Local Setup

### 1. Clone the repo

```bash
git clone https://github.com/woidthevoid/JobtrackerBackend.git
cd JobtrackerBackend
```

### 2. Configure Supabase keys

Edit JobtrackerBackend/appsettings.Development.json with your Supabase credentials:
```JSON
{
  "Supabase": {
    "Url": "https://your-supabase-url.supabase.co",
    "Key": "your-anon-or-service-role-key",
    "ServiceRoleKey": "your-service-role-secret-key"
  }
}
```
### 3. Restore dependencies and run (using .NET CLI)
```bash
dotnet restore
dotnet run --project JobtrackerBackend
```
The API will be available at http://localhost:5050.

### 4. Run with Docker
Build and start the API with Docker:
```bash
cd JobtrackerBackend
docker build -t jobtracker-backend .
docker run --env-file ../JobtrackerBackend/appsettings.Development.json -p 5050:5050 jobtracker-backend
```
Or, using Compose (from project root):
```bash
docker compose up --build
```
---

## 🔒 Authentication
- All endpoints require a JWT Bearer token in the Authorization header.
- Token must be issued by your Supabase project and have the correct audience (authenticated).
- Example:
```Code
Authorization: Bearer <token>
```
Unauthorized/forbidden requests return HTTP 401/403.

## 📖 API Endpoints

### GET /jobs
- Description: Get all job applications for the authenticated user.
- Headers: Authorization: Bearer <token>
- Response: 200 OK
```JSON
[
  {
    "id": "guid",
    "userId": "guid",
    "title": "string",
    "description": "string",
    "jobLink": "string",
    "cvFileUrl": "string",
    "appFileUrl": "string",
    "applicationStatus": "string",
    "createdAt": "2025-01-01T12:00:00Z"
  }
] 
```

### POST /jobs
- Description: Create a new job application (supports CV/application file uploads).
- Headers: Authorization: Bearer <token>
- Request: multipart/form-data
  - title (string, required)
  - description (string, required)
  - jobLink (string, optional)
  - applicationStatus (string, optional)
  - cvFile (file, optional)
  - appFile (file, optional)

- Response:
  - 201 Created with created job DTO
  - 400 Bad Request if fields are missing
  - 401 Unauthorized if token is invalid

```sh
curl -X POST http://localhost:5050/jobs \
  -H "Authorization: Bearer <token>" \
  -F "title=Backend Developer" \
  -F "description=Applied via LinkedIn" \
  -F "jobLink=https://company.com/jobs/123" \
  -F "cvFile=@/path/to/cv.pdf"
```

### DELETE jobs/{id}
- Description: Delete a job application if it belongs to the authenticated user.
- Headers: Authorization: Bearer <token>
- Route Param: id (GUID)
- Response:
  - 200 OK if deleted
  - 404 Not Found if not found or not owned by user
  - 403 Forbidden if unauthorized

 ---

 ## 🐳 Docker & Deployment
 - The project includes a Dockerfile and (optionally) compose.yaml for easy local containerization.
 - Set your Supabase credentials in JobtrackerBackend/appsettings.Development.json or via environment variables.
 - The API listens on port 5050 by default.

## 💬 Questions?
Open an issue at https://github.com/woidthevoid/JobtrackerBackend/issues if you need help or wish to suggest improvements.
