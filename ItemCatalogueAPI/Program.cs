#region Set up the WebApplication builder and register services via per-layer DI modules.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Each layer owns its own DI wiring. See the layer's DependencyInjection.cs for details. This keeps the composition root clean and decoupled from the layers' internal structure.
builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration);

// AddGlobalExceptionHandling registers the IExceptionHandler chain + RFC 9457 problem-details responses.
 builder.Services.AddGlobalExceptionHandling();

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