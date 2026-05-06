using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pix.ClientDemo.Client;
using Pix.ClientDemo.Client.Handlers;
using Pix.ClientDemo.Scenarios;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<PixClientOptions>(builder.Configuration.GetSection(PixClientOptions.SectionName));

builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true
});

builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services.AddTransient<IdempotencyKeyHandler>();
builder.Services.AddTransient<RequestLoggingHandler>();

builder.Services.AddHttpClient("PixServerRaw", (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PixClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.ConfigurePrimaryHttpMessageHandler(MutualTlsHttpHandlerFactory.Create)
.AddStandardResilienceHandler();

builder.Services.AddHttpClient<PixProcessingClient>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PixClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.ConfigurePrimaryHttpMessageHandler(MutualTlsHttpHandlerFactory.Create)
.AddHttpMessageHandler<CorrelationIdHandler>()
.AddHttpMessageHandler<IdempotencyKeyHandler>()
.AddHttpMessageHandler<RequestLoggingHandler>()
.AddStandardResilienceHandler();

builder.Services.AddSingleton<IAuthTokenProvider, AuthTokenProvider>();
builder.Services.AddTransient<PixScenarioRunner>();

var host = builder.Build();

using var scope = host.Services.CreateScope();
var runner = scope.ServiceProvider.GetRequiredService<PixScenarioRunner>();
await runner.RunAsync();
