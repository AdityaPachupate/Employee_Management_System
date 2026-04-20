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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<DepartmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Logging setup (Plug & Play)
builder.AddCentralLogging("DepartmentService");

// MediatR + Pipeline Behaviors
//builder.Services.AddMediatR(cfg => {
//    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
//    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
//});

// FluentValidation
//builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Mapster Configuration
var config = TypeAdapterConfig.GlobalSettings;
config.Scan(Assembly.GetExecutingAssembly());
//builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, Mapper>();

// Repositories
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();