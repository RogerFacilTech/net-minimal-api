using Catalogo.Domain;
using Catalogo.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FacShopAPI.Catalogo.Data;

using FacShopAPI.Catalogo.Application.Interfaces;

public class CatalogoDbContext : DbContext, ICatalogoContext
{
    public CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : base(options)
    {
    }

    public DbSet<Produto> Produtos { get; set; } = null!;
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Variante> Variantes { get; set; } = null!;
    public DbSet<Atributo> Atributos { get; set; } = null!;
    public DbSet<Midia> Midias { get; set; } = null!;

    // ICatalogoContext implementation
    IQueryable<Produto> ICatalogoContext.Produtos => Produtos;
    IQueryable<Categoria> ICatalogoContext.Categorias => Categorias;
    IQueryable<Variante> ICatalogoContext.Variantes => Variantes;
    IQueryable<Atributo> ICatalogoContext.Atributos => Atributos;
    IQueryable<Midia> ICatalogoContext.Midias => Midias;

    void ICatalogoContext.AddProduto(Produto produto) => Produtos.Add(produto);
    void ICatalogoContext.AddCategoria(Categoria categoria) => Categorias.Add(categoria);
    void ICatalogoContext.AddVariante(Variante variante) => Variantes.Add(variante);
    void ICatalogoContext.AddAtributo(Atributo atributo) => Atributos.Add(atributo);
    void ICatalogoContext.AddMidia(Midia midia) => Midias.Add(midia);
    void ICatalogoContext.Remove<T>(T entity) => Set<T>().Remove(entity);
    async Task<int> ICatalogoContext.SaveChangesAsync(CancellationToken cancellationToken) => await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Produto>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nome).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Descricao).IsRequired().HasConversion(
                descricao => descricao.Value,
                value => DescricaoProduto.Reconstituir(value));
            entity.Property(p => p.Preco).HasPrecision(10, 2).IsRequired();
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nome).IsRequired().HasMaxLength(100);
        });

        // Add other entity configurations here...
    }
}