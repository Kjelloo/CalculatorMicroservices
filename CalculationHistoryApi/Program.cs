using CalculationHistoryApi.Data.Database;
using CalculationHistoryApi.Infrastructure;
using CalculationHistoryService.Data.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<CalculationHistoryContext>(opt => opt.UseInMemoryDatabase("CalculationHistoryDb"));

builder.Services.AddScoped<IRepository<Calculation>, CalculationRepository>();

builder.Services.AddTransient<IDbInitializer, DbInitializer>();

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

// Start message listener in a separate thread.
Task.Factory.StartNew(() => new MessageListener(app.Services).Start());

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();