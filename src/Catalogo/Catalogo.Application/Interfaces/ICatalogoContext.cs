using Catalogo.Domain;
using Catalogo.Domain.ValueObjects;

namespace FacShopAPI.Catalogo.Application.Interfaces;

public interface ICatalogoContext
{
    IQueryable<Produto> Produtos { get; }
    IQueryable<Categoria> Categorias { get; }
    IQueryable<Variante> Variantes { get; }
    IQueryable<Atributo> Atributos { get; }
    IQueryable<Midia> Midias { get; }
    void AddProduto(Produto produto);
    void AddCategoria(Categoria categoria);
    void AddVariante(Variante variante);
    void AddAtributo(Atributo atributo);
    void AddMidia(Midia midia);
    void Remove<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
