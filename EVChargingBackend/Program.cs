/*
 * Author: EV Charging System
 * Date: 2025-09-09
 * Purpose: Main program entry point for EV Charging System API
 */

using EVChargingBackend.Data;
using EVChargingBackend.Helpers;
using EVChargingBackend.Middleware;
using EVChargingBackend.Queries;
using EVChargingBackend.Repositories;
using EVChargingBackend.Services;
using EVChargingBackend.Validators;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        // Configure TimeSpan serialization to use string format (HH:mm:ss)
        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
        options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
        // Add custom TimeSpan converter
        options.SerializerSettings.Converters.Add(new TimeSpanConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EV Charging System API",
        Version = "v1",
        Description = "API for EV Charging Station Booking System",
        Contact = new OpenApiContact
        {
            Name = "EV Charging System Team",
            Email = "support@evcharging.com"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configuration
var jwtConfig = builder.Configuration.GetSection("Jwt");
var mongoConfig = builder.Configuration.GetSection("Mongo");
var corsConfig = builder.Configuration.GetSection("Cors");

// MongoDB
builder.Services.AddSingleton<MongoDbContext>(provider =>
    new MongoDbContext(
        mongoConfig["ConnectionString"] ?? "mongodb://localhost:27017",
        mongoConfig["Database"] ?? "ev_charging_db"
    ));

// Repositories
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<EVOwnerRepository>();
builder.Services.AddScoped<ChargingStationRepository>();
builder.Services.AddScoped<BookingRepository>();

// Queries
builder.Services.AddScoped<BookingQueries>();

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<EVOwnerService>();
builder.Services.AddScoped<ChargingStationService>();
builder.Services.AddScoped<BookingService>();

// Helpers
builder.Services.AddScoped<JwtHelper>(provider =>
    new JwtHelper(
        jwtConfig["Issuer"] ?? "EVCharging",
        jwtConfig["Audience"] ?? "EVClients",
        jwtConfig["Key"] ?? "REPLACE_WITH_64_CHAR_RANDOM_KEY_FOR_PRODUCTION_USE_ONLY_123456789012345678901234567890123456789012345678901234567890",
        60 // 60 minutes expiration
    ));

builder.Services.AddScoped<QRCodeGenerator>(provider =>
    new QRCodeGenerator(false)); // Set to true if PNG generation is needed

// Validators
builder.Services.AddScoped<LoginRequestValidator>();
builder.Services.AddScoped<RegisterRequestValidator>();
builder.Services.AddScoped<UserRequestValidator>();
builder.Services.AddScoped<EVOwnerRequestValidator>();
builder.Services.AddScoped<ChargingStationRequestValidator>();
builder.Services.AddScoped<StationScheduleRequestValidator>();
builder.Services.AddScoped<BookingRequestValidator>();
builder.Services.AddScoped<BookingUpdateRequestValidator>();
builder.Services.AddScoped<BookingCompleteRequestValidator>();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"] ?? "REPLACE_WITH_64_CHAR_RANDOM_KEY_FOR_PRODUCTION_USE_ONLY_123456789012345678901234567890123456789012345678901234567890")),
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BackofficeOnly", policy => policy.RequireRole("Backoffice"));
    options.AddPolicy("OperatorOrBackoffice", policy => policy.RequireRole("Backoffice", "StationOperator"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigins", policy =>
    {
        var origins = corsConfig.GetSection("Origins").Get<string[]>() ?? new[] { "http://localhost:5173", "http://localhost:3000" };
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    
    // Development CORS policy - allow all origins
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger for all environments for debugging
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EV Charging System API v1");
    c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger endpoint
});

// CORS - Must be before HTTPS redirection
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowOrigins");
}

// Disable HTTPS redirection in development to avoid CORS issues
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Error handling middleware
app.UseErrorHandling();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Create database indexes on startup
using (var scope = app.Services.CreateScope())
{
    var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await mongoContext.CreateIndexesAsync();

    // Create default admin user if none exists
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    await userService.CreateDefaultAdminAsync();
}

app.Run();

/// <summary>
/// Custom JSON converter for TimeSpan objects to serialize as HH:mm:ss format
/// </summary>
public class TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString(@"hh\:mm\:ss"));
    }

    public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            var stringValue = reader.Value?.ToString();
            if (TimeSpan.TryParse(stringValue, out var timeSpan))
            {
                return timeSpan;
            }
        }
        return TimeSpan.Zero;
    }
}
