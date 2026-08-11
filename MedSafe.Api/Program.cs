using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MedSafeAPI.BackgroundServices;
using MedSafe.Infrastructure.Data;
using MedSafe.Infrastructure.Interfaces;
using MedSafe.Infrastructure.Repository;
using MedSafe.Logging.MiddleWares;
using MedSafeAPI.Filter;
using MedSafeAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        b => b.MigrationsAssembly("MedSafe.Api")));

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IIncidentReportService, IncidentReportService>();
builder.Services.AddScoped<IIncidentAttachmentService, IncidentAttachmentService>();
builder.Services.AddScoped<IAlertService, AlertService>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Background Services
builder.Services.AddHostedService<AlertMonitorService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!;
builder.Services.AddCors(opt =>
    opt.AddPolicy("MedSafeCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Controllers + Global Exception Filter
builder.Services.AddControllers(opt =>
    opt.Filters.Add<GlobalExceptionFilter>());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("MedSafeCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();
app.MapControllers();

app.Run();
