using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using FacShopAPI.Shared.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pix.ClientDemo.Client;
using Pix.ClientDemo.Scenarios;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<PixClientOptions>(builder.Configuration.GetSection(PixClientOptions.SectionName));

builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true
});

builder.Services.AddSharedHttpInfrastructure(options =>
{
    options.PathContains = ["/pix/v1/cobrancas"];
});

builder.Services.AddHttpClient("PixServerRaw", (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PixClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.ConfigurePrimaryHttpMessageHandler(MutualTlsHttpHandlerFactory.Create)
.AddDefaultApiResiliencePipeline("pix-raw", new ApiClientOptionsBase());

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
.AddDefaultApiResiliencePipeline("pix", new ApiClientOptionsBase());

builder.Services.AddSingleton<IAuthTokenProvider, AuthTokenProvider>();
builder.Services.AddTransient<PixScenarioRunner>();

var host = builder.Build();

using var scope = host.Services.CreateScope();
var runner = scope.ServiceProvider.GetRequiredService<PixScenarioRunner>();
await runner.RunAsync();
