using EVChargingBackend.Services;
using EVChargingBackend.Models;
using EVChargingBackend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register our services
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<JwtHelper>();

// Register MongoDB context
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? 
    builder.Configuration["Mongo:ConnectionString"];
var mongoDatabase = builder.Configuration["Mongo:Database"] ?? "ev_charging_db";

builder.Services.AddSingleton<MongoDbContext>(provider => 
    new MongoDbContext(mongoConnectionString, mongoDatabase));

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
            ValidIssuer = "EVCharging",
            ValidAudience = "EVClients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("dev-secret-key-change"))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Dev: no HTTPS redirect to allow http://10.0.2.2
    // Swagger on
    app.UseSwagger();
    app.UseSwaggerUI();

    // In-memory seed
    var store = app.Services.GetRequiredService<UserStore>();
    EVChargingBackend.Services.DevSeeder.Seed(store.Users);
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "OK" }));

app.Run();