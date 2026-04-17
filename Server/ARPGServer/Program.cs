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
    }

    app.MapOpenApi();

    app.MapGet("/api/debug/db-check", async (AppDbContext db) => Results.Ok(new
    {
        database = db.Database.GetDbConnection().DataSource,
        users = await db.Users.CountAsync(),
        cloudSaves = await db.CloudSaves.CountAsync()
    }))
    .WithName("DatabaseCheck");
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
