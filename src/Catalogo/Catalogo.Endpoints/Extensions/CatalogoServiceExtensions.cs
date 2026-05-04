using FluentValidation;
using ProdutosAPI.Catalogo.Application.Repositories;
using ProdutosAPI.Catalogo.Application.Services;
using ProdutosAPI.Catalogo.Application.Validators;
using ProdutosAPI.Catalogo.Infrastructure.Queries;
using ProdutosAPI.Catalogo.Infrastructure.Repositories;

namespace ProdutosAPI.Catalogo.Endpoints.Extensions;

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
