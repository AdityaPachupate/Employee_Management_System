using MediatR;
using FluentValidation;
using Mapster;
using MapsterMapper;
using System.Reflection;
//using DepartmentService.Application.Common.Behaviors;
using DepartmentService.Application.Common.Interfaces;
using DepartmentService.Infrastructure.Persistence;
using DepartmentService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

using Shared.Logging;
using DepartmentService.Domain;
using Shared.Messaging;
using DepartmentService.Infrastructure.Messaging;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<DepartmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<DepartmentDbContext>();

// Logging setup (Plug & Play)
builder.AddCentralLogging("DepartmentService");
builder.Logging.AddCentralLogger(
    serviceName: "DepartmentService",
    loggingUrl: builder.Configuration["LoggingServiceUrl"]!
);



// Mapster Configuration
var config = TypeAdapterConfig.GlobalSettings;
config.Scan(Assembly.GetExecutingAssembly());
//builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, Mapper>();

// Repositories
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();

// --- MESSAGING SETUP (MassTransit) ---
// This registers the 'Bus' that handles sending and receiving messages.
builder.Services.AddMassTransit(x =>
{
    // Register our Consumers so the system knows what to do when a message arrives.
    x.AddConsumer<EmployeeEventConsumer>();
    x.AddConsumer<LocalLogConsumer>();

    // For local development in Visual Studio, we use 'InMemory' mode.
    // This allows messages to flow WITHOUT needing to install RabbitMQ or Docker!
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DepartmentService API", Version = "v1" });

    // Enable JWT Authentication in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
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
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

