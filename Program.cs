using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using VeltrexAPI.Data;
using VeltrexAPI.Infrastructure;
using VeltrexAPI.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

/* ---------- Database (EF Core code-first, MySQL) ---------- */
var connectionString = config.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:Default is not set in appsettings.json.");
// Set explicitly (instead of auto-detect) so startup and `dotnet ef` don't need a live connection.
var mySqlVersion = ServerVersion.Parse(config["Database:MySqlVersion"] ?? "8.0.36-mysql");
builder.Services.AddDbContext<AppDbContext>(o => o.UseMySql(connectionString, mySqlVersion));

/* ---------- Auth (JWT bearer) ---------- */
var jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be set to a secret of at least 32 characters (appsettings or user-secrets).");
builder.Services.Configure<JwtOptions>(config.GetSection("Jwt"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // keep "sub" / "role" claim names as issued
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
        // Same { message } shape as every other error.
        o.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { message = "Please log in again" });
            },
            OnForbidden = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return ctx.Response.WriteAsJsonAsync(new { message = "You don't have access to this" });
            },
        };
    });
builder.Services.AddAuthorization();

/* ---------- App services ---------- */
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<MarketService>();
builder.Services.AddSingleton<QrStorage>();

/* ---------- MVC + JSON ---------- */
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
        // Enums as BUY / SELL / PENDING / CREDIT ... to match the frontend types.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)))
    .ConfigureApiBehaviorOptions(o =>
        // Validation errors as { message } with the first problem, which the UI shows directly.
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState.Where(e => e.Value?.Errors.Count > 0).ToList();
            // Malformed JSON values (e.g. side: "HOLD") are keyed "$.side"; name the field instead of the raw parser text.
            var jsonError = errors.FirstOrDefault(e => e.Key.StartsWith('$'));
            var message = jsonError.Key is { Length: > 1 } key
                ? $"Invalid value for '{key.TrimStart('$', '.')}'"
                : errors.SelectMany(e => e.Value!.Errors).Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Invalid request";
            return new BadRequestObjectResult(new { message });
        });

/* ---------- CORS for the React app ---------- */
var origins = config.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

/* ---------- Swagger ---------- */
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Veltrex API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the token returned by POST /api/auth/login",
    });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", doc)] = [] });
});

var app = builder.Build();

/* ---------- Apply migrations + seed on startup ---------- */
if (config.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, config, logger);
}

/* ---------- Pipeline ---------- */
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.DocumentTitle = "Veltrex API");
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(); // serves wwwroot/uploads/qr/*
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
