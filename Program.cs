using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaskFlowAPI.Data;
using TaskFlowAPI.Middleware;
using TaskFlowAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Azure Key Vault (Step 4 — uncomment when ready) ───────────────────────
// var keyVaultUri = builder.Configuration["KeyVaultUri"];
// if (!string.IsNullOrEmpty(keyVaultUri))
// {
//     builder.Configuration.AddAzureKeyVault(
//         new Uri(keyVaultUri),
//         new DefaultAzureCredential()
//     );
// }

// ── 2. Application Insights (Step 6 — uncomment when ready) ──────────────────
// builder.Services.AddApplicationInsightsTelemetry();

// ── 3. Database ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount:       5,
            maxRetryDelay:       TimeSpan.FromSeconds(10),
            errorNumbersToAdd:   null
        )
    )
);

// ── 4. JWT Authentication ─────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer           = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidateAudience         = true,
        ValidAudience            = jwtSettings["Audience"],
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero       // No grace period on expiry
    };

    // Surface auth errors in response headers (useful during development)
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            ctx.Response.Headers["X-Auth-Failed"] = ctx.Exception.Message;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ── 5. Services ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddControllers();

// ── 6. CORS (adjust origins for production) ───────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ── 7. Swagger with JWT support ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "TaskFlow API",
        Version = "v1",
        Description = "Cloud-native task management API — .NET 8, Azure App Service, Docker"
    });

    // Allow pasting a JWT directly into Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token (without 'Bearer ' prefix)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── 8. Auto-run EF migrations on startup ──────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ── 9. Middleware pipeline ────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
        c.RoutePrefix = string.Empty;      // Swagger at root URL
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();               // Must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
