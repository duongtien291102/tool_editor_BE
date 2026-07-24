# AI Video Studio — Enterprise Backend

AI Video Studio is an enterprise-grade AI video generation, composition, orchestration, and rendering platform built with .NET 9, Clean Architecture, DDD, CQRS, MongoDB, and Zero Trust Security.

---

## Environment Variables

The backend application requires MongoDB configuration supplied via Environment Variables or a `.env` file located at the project root (`tool_editor_BE/.env` or workspace root `.env`).

### Required Variables

- `MONGODB_CONNECTION_STRING`: The full MongoDB connection string (e.g. `mongodb+srv://<user>:<password>@<cluster>.mongodb.net/<database>?retryWrites=true&w=majority`).
- `MONGODB_DATABASE`: The target MongoDB database name (default: `AiVideoStudio`).

### How to Setup `.env` File

1. Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```

2. Open `.env` and configure your credentials:
   ```env
   MONGODB_CONNECTION_STRING=mongodb+srv://duongtien291102_db_user:123456editor@tooleditor.uibveyr.mongodb.net/AiVideoStudio?retryWrites=true&w=majority&appName=tooleditor
   MONGODB_DATABASE=AiVideoStudio
   ```

---

## Getting Started

### Prerequisites
- .NET 9 SDK
- MongoDB 7.0+ (or MongoDB Atlas cluster)

### Running the Backend
```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/AiVideoStudio.Api
```
