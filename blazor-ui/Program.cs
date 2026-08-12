using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Render App into the DOM node matching this CSS selector. This is the main component of the Blazor application.
builder.RootComponents.Add<App>("#app");

// A second root component. "head::after" selects <head>, and the ::after suffix means "append into it"
// rather than replace its children - which is what the bare "#app" selector above does, and why the
// loading spinner gets wiped. HeadOutlet is what makes <PageTitle> and <HeadContent> work from any page.
builder.RootComponents.Add<HeadOutlet>("head::after");

// In Standalone WASM there is one DI container for the life of the tab, and exactly one scope - created at
// startup and never replaced. So Scoped behaves like Singleton unless you create a scope yourself via
// IServiceScopeFactory. Registering as Scoped keeps the code portable to Blazor Server, where a scope is one
// user's circuit.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
