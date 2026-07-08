using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = FunctionsApplication.CreateBuilder(args);

builder.Services
    .AddApplication(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

builder.Build().Run();
