using ItemCatalogueAPI.ExceptionHandling;
using ItemCatalogueAPI.Extensions;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Interceptors;


#region Set up the WebApplication builder, register services, and configure the DbContext with the auditing interceptor.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplicationServices();

// Register the centralized exception handling
builder.Services.AddGlobalExceptionHandling();

// Single clock source for all audit stamping (CreatedDate/LastModifiedDate). Injected into both the auditing interceptor and ItemRepository's ExecuteUpdate soft-delete
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AuditingSaveChangesInterceptor>();

builder.Services.AddDbContext<ItemCatalogueDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("local"))
           .AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>()));

#endregion


#region Build the Http middleware pipeline and run the app
var app = builder.Build();

// Terminal exception handler: catches everything downstream and routes it through the IExceptionHandler chain. Registered first so it wraps the rest of the pipeline.
// Runs after builder.Build() so it can be terminal and wrap the entire pipeline, but before any other middleware so it can catch exceptions from them.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
#endregion