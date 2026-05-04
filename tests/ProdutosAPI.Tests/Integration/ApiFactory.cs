using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProdutosAPI.Catalogo.Endpoints.Extensions;
using ProdutosAPI.Shared.Data;
using ProdutosAPI.Catalogo.Infrastructure.Data;

namespace ProdutosAPI.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Program.cs pula AddCatalogoRateLimiting() em Testing; cada factory registra seus próprios limites
        builder.ConfigureServices(AddRateLimiting);
    }

    // Limites altos para não interferir em testes funcionais existentes
    protected virtual void AddRateLimiting(IServiceCollection services) =>
        services.AddCatalogoRateLimitingWithLimits(leituraLimit: 10000, escritaLimit: 10000, criacaoProdutoLimit: 10000);

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        DbSeeder.Seed(db);
        return host;
    }
}
