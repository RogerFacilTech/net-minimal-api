using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Application.Services;
using FacShopAPI.Catalogo.Application.Validators;
using FacShopAPI.Catalogo.Infrastructure.Queries;
using FacShopAPI.Catalogo.Infrastructure.Repositories;
using FluentValidation;

namespace FacShopAPI.Catalogo.Endpoints.Extensions;

public static class CatalogoServiceExtensions
{
    public static IServiceCollection AddCatalogo(this IServiceCollection services)
    {
        // Produto
        services.AddScoped<IProdutoService, ProdutoService>();
        services.AddScoped<IProdutoQueryRepository, DapperProdutoQueryRepository>();
        services.AddScoped<IProdutoCommandRepository, EfProdutoCommandRepository>();

        // Categoria
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<ICategoriaQueryRepository, DapperCategoriaQueryRepository>();
        services.AddScoped<ICategoriaCommandRepository, EfCategoriaCommandRepository>();

        // Variante
        services.AddScoped<IVarianteService, VarianteService>();
        services.AddScoped<IVarianteQueryRepository, DapperVarianteQueryRepository>();
        services.AddScoped<IVarianteCommandRepository, EfVarianteCommandRepository>();

        // Atributo
        services.AddScoped<IAtributoService, AtributoService>();
        services.AddScoped<IAtributoRepository, EfAtributoRepository>();

        // Mídia
        services.AddScoped<IMidiaService, MidiaService>();
        services.AddScoped<IMidiaRepository, EfMidiaRepository>();

        services.AddValidatorsFromAssemblyContaining<CriarProdutoValidator>();
        return services;
    }
}
