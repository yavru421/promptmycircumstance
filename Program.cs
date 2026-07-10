using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PromptMyCircumstance;
using PromptMyCircumstance.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Crucible Engine Services
builder.Services.AddScoped<BalancedScoringEngine>();
builder.Services.AddScoped<ChallengeLibrary>();

await builder.Build().RunAsync();

