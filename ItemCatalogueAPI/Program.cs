using ItemCatalogueAPI.Extensions;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Interceptors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register application services and repositories
builder.Services.AddApplicationServices();

// Single clock source for all audit stamping (CreatedDate/LastModifiedDate). Injected into both
// the auditing interceptor and ItemRepository's ExecuteUpdate soft-delete path so every
// app-driven write shares one clock. Swappable with FakeTimeProvider in tests.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AuditingSaveChangesInterceptor>();

// Register DbContext
builder.Services.AddDbContext<ItemCatalogueDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("local"))
           .AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
