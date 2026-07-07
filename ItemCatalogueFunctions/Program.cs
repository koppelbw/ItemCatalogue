using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Composition root for the queue-side of bulk import. The Functions host is just another driving
// adapter over the same layers as the API: it reuses the per-layer DI modules verbatim, so the
// import core (validation, FK resolution, chunk persistence) is wired identically in both hosts
// and no business logic lives here.
var builder = FunctionsApplication.CreateBuilder(args);

builder.Services
    .AddApplication(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

builder.Build().Run();
