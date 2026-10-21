using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var dbSettings = builder.Configuration.GetSection("GastoApiSettings:DbSettings");
var connectionString = new SqlConnectionStringBuilder
{
    DataSource = dbSettings["ServerName"],
    InitialCatalog = dbSettings["DatabaseName"],
    IntegratedSecurity = dbSettings.GetValue<bool>("UseIntegratedSecurity"),
    MultipleActiveResultSets = dbSettings.GetValue<bool>("MultipleActiveResultSets"),
    Encrypt = dbSettings.GetValue<bool>("Encrypt"),
    TrustServerCertificate = dbSettings.GetValue<bool>("TrustServerCertificate")
}.ConnectionString;

builder.Services.AddDbContextPool<Finanzas.Data.FinanzasDbContext>(options =>
    options.UseSqlServer(connectionString));

var jwtSection = builder.Configuration.GetSection("GastoApiSettings:JwtSettings");
builder.Services.Configure<Finanzas.Api.Models.JwtSettings>(jwtSection);
var jwtSettings = jwtSection.Get<Finanzas.Api.Models.JwtSettings>()
    ?? throw new Exception("No se encontró GastoApiSettings:JwtSettings en appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<Finanzas.Api.Services.JwtManager>();
builder.Services.AddScoped<Finanzas.Api.Services.UsuarioService>();
builder.Services.AddScoped<Finanzas.Api.Services.CuentaService>();
builder.Services.AddScoped<Finanzas.Api.Services.CategoriaService>();
builder.Services.AddScoped<Finanzas.Api.Services.PersonaService>();
builder.Services.AddScoped<Finanzas.Api.Services.OperacionService>();
builder.Services.AddScoped<Finanzas.Api.Services.DashboardService>();
builder.Services.AddScoped<Finanzas.Api.Services.CuentaDetalleService>();
builder.Services.AddScoped<Finanzas.Api.Services.MsiService>();
builder.Services.AddScoped<Finanzas.Api.Services.AnalisisService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    var origenes = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    options.AddPolicy("Blazor", policy =>
        policy.WithOrigins(origenes.Select(o => o.TrimEnd('/')).ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // En desarrollo la app Blazor llama por http; la redirección rompería el preflight de CORS.
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("Blazor");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
