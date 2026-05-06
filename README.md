TaskFlow API — Step 1: .NET Core 8 Web API
Project Structure
```
TaskFlowAPI/
├── Controllers/
│   ├── AuthController.cs       # POST /api/auth/register, /login
│   └── TasksController.cs      # Full CRUD — GET, POST, PUT, DELETE /api/tasks
├── Data/
│   └── AppDbContext.cs         # EF Core DbContext + model config
├── DTOs/
│   └── Dtos.cs                 # Request/Response records
├── Middleware/
│   └── ExceptionMiddleware.cs  # Global exception handler
├── Models/
│   ├── User.cs                 # User entity + Role enum
│   └── TaskItem.cs             # TaskItem entity + Status/Priority enums
├── Services/
│   └── JwtService.cs           # Token generation + validation
├── Program.cs                  # App bootstrap + DI + middleware pipeline
├── appsettings.json            # Local dev config
└── appsettings.Production.json # Prod overrides (secrets via Key Vault in Step 4)
```
API Endpoints
Auth
Method	Endpoint	Auth	Description
POST	/api/auth/register	None	Register new user
POST	/api/auth/login	None	Login, get JWT
Tasks
Method	Endpoint	Auth	Description
GET	/api/tasks	User / Admin	Get own tasks (filtered)
GET	/api/tasks/{id}	User / Admin	Get task by ID
POST	/api/tasks	User / Admin	Create a task
PUT	/api/tasks/{id}	User / Admin	Update a task
DELETE	/api/tasks/{id}	User / Admin	Delete a task
GET	/api/tasks/all-users	Admin only	Get all tasks all users
Running Locally
Prerequisites
.NET 8 SDK
SQL Server (local) or Azure SQL
1. Restore packages
```bash
dotnet restore
```
2. Update connection string
Edit `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TaskFlowDB;..."
}
```
3. Run EF migrations
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
4. Run the API
```bash
dotnet run
```
Swagger UI available at: `http://localhost:5000`
Key Design Decisions
Decision	Reason
Records for DTOs	Immutable, concise, no boilerplate
BCrypt for passwords	Industry standard, salted by default
Enum → string in DB	Human-readable values in Azure SQL
Auto-migrate on startup	Seamless Azure App Service deployments
ExceptionMiddleware	Consistent JSON error responses across all endpoints
ClockSkew = Zero	Tokens expire exactly on time — no grace period
Next Steps
Step 2: Dockerfile (multi-stage build)
Step 3: Azure Container Registry
Step 4: Azure App Service + Key Vault
Step 5: Azure DevOps CI/CD pipeline
Step 6: Application Insights