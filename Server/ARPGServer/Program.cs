using ARPGServer.Data;
using ARPGServer.Endpoints;
using ARPGServer.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseSqlite(connectionString);
});
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt config is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        EnsureLegacySchema(db);
    }

    app.MapOpenApi();

    app.MapGet("/api/debug/db-check", async (AppDbContext db) => Results.Ok(new
    {
        database = db.Database.GetDbConnection().DataSource,
        users = await db.Users.CountAsync(),
        cloudSaves = await db.CloudSaves.CountAsync()
    }))
    .WithName("DatabaseCheck");

    app.MapGet("/api/debug/users", async (AppDbContext db) =>
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderBy(user => user.CreatedAtUtc)
            .Select(user => new
            {
                id = user.Id,
                userName = user.UserName,
                phoneNumber = user.PhoneNumber,
                createdAtUtc = user.CreatedAtUtc
            })
            .ToListAsync();

        return Results.Ok(users);
    })
    .WithName("DebugUsers");

    app.MapGet("/api/debug/cloud-saves", async (AppDbContext db) =>
    {
        var saves = await db.CloudSaves
            .AsNoTracking()
            .Join(
                db.Users.AsNoTracking(),
                save => save.UserId,
                user => user.Id,
                (save, user) => new
                {
                    userId = save.UserId,
                    userName = user.UserName,
                    version = save.Version,
                    updatedAtUtc = save.UpdatedAtUtc,
                    saveSize = save.SaveJson.Length
                })
            .OrderByDescending(item => item.updatedAtUtc)
            .ToListAsync();

        return Results.Ok(saves);
    })
    .WithName("DebugCloudSaves");

    app.MapGet("/api/debug/cloud-save/{userName}", async (string userName, bool includeJson, AppDbContext db) =>
    {
        var normalizedUserName = (userName ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedUserName))
        {
            return Results.BadRequest(new { code = "INVALID_USER_NAME", message = "User name is required." });
        }

        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.UserName == normalizedUserName);
        if (user == null)
        {
            return Results.NotFound(new { code = "USER_NOT_FOUND", message = "User not found." });
        }

        var save = await db.CloudSaves
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.UserId == user.Id);
        if (save == null)
        {
            return Results.NotFound(new { code = "SAVE_NOT_FOUND", message = "Cloud save not found." });
        }

        return Results.Ok(new
        {
            userId = user.Id,
            userName = user.UserName,
            version = save.Version,
            updatedAtUtc = save.UpdatedAtUtc,
            saveSize = save.SaveJson.Length,
            saveJson = includeJson ? save.SaveJson : null
        });
    })
    .WithName("DebugCloudSaveByUserName");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    name = "ARPG Server",
    version = "0.1.0"
}));

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    timeUtc = DateTime.UtcNow
}))
.WithName("HealthCheck");

app.MapGet("/api/ping", () => Results.Ok(new
{
    message = "pong"
}))
.WithName("Ping");

app.MapAuthEndpoints();
app.MapSaveEndpoints();

app.Run();

static void EnsureLegacySchema(AppDbContext db)
{
    var connection = db.Database.GetDbConnection();
    var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
    if (shouldCloseConnection)
    {
        connection.Open();
    }

    try
    {
        bool hasPhoneNumber = false;
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "PRAGMA table_info('Users');";
            using var reader = checkCmd.ExecuteReader();
            while (reader.Read())
            {
                var columnName = reader["name"]?.ToString();
                if (string.Equals(columnName, "PhoneNumber", StringComparison.OrdinalIgnoreCase))
                {
                    hasPhoneNumber = true;
                    break;
                }
            }
        }

        if (!hasPhoneNumber)
        {
            using var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE Users ADD COLUMN PhoneNumber TEXT NOT NULL DEFAULT '';";
            alterCmd.ExecuteNonQuery();
        }
    }
    finally
    {
        if (shouldCloseConnection)
        {
            connection.Close();
        }
    }
}
