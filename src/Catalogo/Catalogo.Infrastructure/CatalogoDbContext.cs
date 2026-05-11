using Catalogo.Domain;
using Catalogo.Domain.ValueObjects;
using FacShopAPI.Catalogo.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FacShopAPI.Catalogo.Infrastructure;

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

    // ICatalogoContext
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
    async Task<int> ICatalogoContext.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Produto>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.HasIndex(p => p.Ativo).HasDatabaseName("idx_produto_ativo");
            entity.HasIndex(p => p.Categoria).HasDatabaseName("idx_produto_categoria");

            entity.Property(p => p.Nome).IsRequired().HasMaxLength(100)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.Descricao).IsRequired().HasMaxLength(500)
                .HasConversion(d => d.Value, v => DescricaoProduto.Reconstituir(v))
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.Preco).HasPrecision(10, 2).IsRequired()
                .HasConversion(p => p.Value, v => PrecoProduto.Reconstituir(v))
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.Categoria).IsRequired().HasMaxLength(50)
                .HasConversion(c => c.Value, v => CategoriaProduto.Reconstituir(v))
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.Estoque).IsRequired()
                .HasConversion(e => e.Value, v => EstoqueProduto.Reconstituir(v))
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.ContatoEmail).IsRequired().HasMaxLength(100)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.Ativo).HasDefaultValue(true)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(p => p.DataAtualizacao).UsePropertyAccessMode(PropertyAccessMode.Property);
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nome).IsRequired().HasMaxLength(100)
                .UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(c => c.Slug).IsRequired().HasMaxLength(120)
                .UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(c => c.CategoriaPaiId).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(c => c.Ativa).HasDefaultValue(true)
                .UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(c => c.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(c => c.DataAtualizacao).UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("idx_categoria_slug");

            entity.HasOne<Categoria>().WithMany()
                .HasForeignKey(c => c.CategoriaPaiId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });

        modelBuilder.Entity<Atributo>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.ProdutoId).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(a => a.Chave).IsRequired().HasMaxLength(50).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(a => a.Valor).IsRequired().HasMaxLength(200).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(a => a.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
        });

        modelBuilder.Entity<Midia>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.ProdutoId).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(m => m.Url).IsRequired().HasMaxLength(500).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(m => m.Tipo).HasConversion<string>().UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(m => m.Ordem).HasDefaultValue(0).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(m => m.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
        });

        modelBuilder.Entity<Variante>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.ProdutoId).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(v => v.Sku).IsRequired().HasMaxLength(20)
                .HasConversion(sku => sku.Valor, valor => SKU.Reconstituir(valor))
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.HasIndex(v => new { v.ProdutoId, v.Sku }).IsUnique()
                .HasDatabaseName("idx_variante_produto_sku");

            entity.Property(v => v.Descricao).IsRequired().HasMaxLength(200)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(v => v.PrecoAdicional).HasPrecision(10, 2)
                .HasConversion(p => p.Value, v => PrecoProduto.Reconstituir(v))
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(v => v.Estoque)
                .HasConversion(e => e.Value, v => EstoqueProduto.Reconstituir(v))
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(v => v.Ativa).HasDefaultValue(true).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(v => v.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(v => v.DataAtualizacao).UsePropertyAccessMode(PropertyAccessMode.Property);
        });
    }
}
