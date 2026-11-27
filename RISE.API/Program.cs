using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RISE.API.Data;
using RISE.API.Models;
using RISE.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// SQL Server Database Connection
var dbConnection = Environment.GetEnvironmentVariable("RISE_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<RiseDbContext>(options =>
    options.UseSqlServer(dbConnection));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "RISE_Default_Secret_Key_Must_Be_Changed_In_Production_2026";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "RISE",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "RISE-Frontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("OfficerOrAdmin", policy => policy.RequireRole("DA Officer", "Administrator"));
    options.AddPolicy("AnyAuthenticated", policy => policy.RequireAuthenticatedUser());
});

// Controllers
builder.Services.AddControllers();

// Custom services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<RISE.API.Services.NotificationService>();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDevelopment", policy =>
        policy.WithOrigins(
                "http://localhost:5500",
                "http://127.0.0.1:5500",
                "http://localhost:5501",
                "http://127.0.0.1:5501",
                "http://localhost:5502",
                "http://127.0.0.1:5502",
                "http://localhost:5503",
                "http://127.0.0.1:5503"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RiseDbContext>();
    dbContext.Database.Migrate();
    SeedDatabase(scope.ServiceProvider);
}

// Configure HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS must run before controller endpoints.
app.UseCors("FrontendDevelopment");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Automated audit trail for mutating requests
app.Use(async (context, next) =>
{
    await next();
    var method = context.Request.Method;
    if (method == "GET" || context.Response.StatusCode >= 400) return;
    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/uploads")) return;
    using var scope = context.RequestServices.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RiseDbContext>();
    var userIdClaim = context.User.FindFirst("userId")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    int? userId = int.TryParse(userIdClaim, out var uid) ? uid : null;
    db.AuditLogs.Add(new RISE.API.Models.AuditLog
    {
        UserId = userId,
        Action = method,
        EntityName = path,
        EntityId = null,
        OccurredAtUtc = DateTime.UtcNow,
        IpAddress = context.Connection.RemoteIpAddress?.ToString()
    });
    await db.SaveChangesAsync();
});

app.MapControllers();

app.Run();

static void SeedDatabase(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<RiseDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

    // Ensure Administrator role exists
    var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Administrator");
    if (adminRole is null)
    {
        adminRole = new Role { Name = "Administrator", Description = "Full system access" };
        context.Roles.Add(adminRole);
        context.SaveChanges();
    }

    // Ensure DA Officer role exists
    var officerRole = context.Roles.FirstOrDefault(r => r.Name == "DA Officer");
    if (officerRole is null)
    {
        officerRole = new Role { Name = "DA Officer", Description = "Department of Agriculture officer" };
        context.Roles.Add(officerRole);
        context.SaveChanges();
    }

    // Seed default supplies if none exist
    if (!context.Supplies.Any())
    {
        var category = context.SupplyCategories.FirstOrDefault(c => c.Name == "Seeds");
        if (category is null)
        {
            category = new SupplyCategory { Name = "Seeds", Description = "Rice seeds and planting materials" };
            context.SupplyCategories.Add(category);
            context.SaveChanges();
        }
        context.Supplies.Add(new Supply { Name = "Certified Rice Seeds", UnitOfMeasure = "kg", UnitCost = 45.00m, StockQuantity = 1000, SupplyCategoryId = category.SupplyCategoryId, IsActive = true });
        context.Supplies.Add(new Supply { Name = "Urea Fertilizer", UnitOfMeasure = "bag", UnitCost = 1200.00m, StockQuantity = 500, SupplyCategoryId = category.SupplyCategoryId, IsActive = true });
        context.SaveChanges();
    }

    // Seed default Administrator if none exists
    if (!context.Users.Any(u => u.UserRoles.Any(ur => ur.Role.Name == "Administrator")))
    {
        var adminUser = new User
        {
            UserName = "admin",
            Email = "admin@rise.gov.ph",
            DisplayName = "System Administrator",
            PasswordHash = passwordService.HashPassword("Admin@123"),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Users.Add(adminUser);
        context.SaveChanges();

        context.UserRoles.Add(new UserRole { UserId = adminUser.UserId, RoleId = adminRole.RoleId, AssignedAtUtc = DateTime.UtcNow });
        context.SaveChanges();
    }

    // Assign any legacy/seeded records to the administrator account.
    var adminId = context.Users
        .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Administrator"))
        .Select(u => u.UserId).FirstOrDefault();
    if (adminId > 0)
    {
        context.Farmers.Where(f => f.OwnerUserId == 0).ExecuteUpdate(f => f.SetProperty(x => x.OwnerUserId, adminId));
        context.Supplies.Where(s => s.OwnerUserId == 0).ExecuteUpdate(s => s.SetProperty(x => x.OwnerUserId, adminId));
        context.SupplyCategories.Where(c => c.OwnerUserId == 0).ExecuteUpdate(c => c.SetProperty(x => x.OwnerUserId, adminId));
    }
}
