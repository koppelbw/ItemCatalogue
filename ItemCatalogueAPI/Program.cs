#region Set up the WebApplication builder and register services via per-layer DI modules.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Each layer owns its own DI wiring. See the layer's DependencyInjection.cs for details. This keeps the composition root clean and decoupled from the layers' internal structure.
builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration);

// AddObservability wires OpenTelemetry (traces/metrics/logs) with auto-instrumentation, exporting to
// Application Insights in Azure (connection string) or OTLP locally (Aspire dashboard / collector).
builder.Services.AddObservability(builder.Configuration, builder.Environment);

// AddGlobalExceptionHandling registers the IExceptionHandler chain + RFC 9457 problem-details responses.
 builder.Services.AddGlobalExceptionHandling();

builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services.AddScheduledReset(builder.Configuration);

// Cross-origin: allow the deployed Static Web App to call the API. Origins come from
// config (Cors:AllowedOrigins) — set in Azure as Cors__AllowedOrigins__0.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

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
app.UseCors();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();
#endregion