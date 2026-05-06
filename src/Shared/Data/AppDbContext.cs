using Catalogo.Domain;
using Catalogo.Domain.ValueObjects;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Pedidos.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacShopAPI.Shared.Data;

public class AppDbContext : DbContext, ICatalogoContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Variante> Variantes => Set<Variante>();
    public DbSet<Atributo> Atributos => Set<Atributo>();
    public DbSet<Midia> Midias => Set<Midia>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();

    // ICatalogoContext: explicit implementation — DbSet<T> não satisfaz IQueryable<T> implicitamente
    IQueryable<Produto> ICatalogoContext.Produtos => Set<Produto>();
    IQueryable<Categoria> ICatalogoContext.Categorias => Set<Categoria>();
    IQueryable<Variante> ICatalogoContext.Variantes => Set<Variante>();
    IQueryable<Atributo> ICatalogoContext.Atributos => Set<Atributo>();
    IQueryable<Midia> ICatalogoContext.Midias => Set<Midia>();

    public void AddProduto(Produto produto) => this.Add(produto);
    public void AddCategoria(Categoria categoria) => this.Add(categoria);
    public void AddVariante(Variante variante) => this.Add(variante);
    public void AddAtributo(Atributo atributo) => this.Add(atributo);
    public void AddMidia(Midia midia) => this.Add(midia);
    public new void Remove<T>(T entity) where T : class => base.Remove(entity!);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração da entidade Produto
        modelBuilder.Entity<Produto>(entity =>
        {
            entity.HasKey(p => p.Id);

            // Índices para melhor performance
            // Referência: Melhores-Praticas-API.md - Seção "Performance"
            entity.HasIndex(p => p.Ativo)
                .HasDatabaseName("idx_produto_ativo");

            entity.HasIndex(p => p.Categoria)
                .HasDatabaseName("idx_produto_categoria");

            // Configuração de propriedades
            entity.Property(p => p.Nome)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Descricao)
                .IsRequired()
                .HasConversion(
                    descricao => descricao.Value,
                    value => DescricaoProduto.Reconstituir(value))
                .HasMaxLength(500);

            entity.Property(p => p.Preco)
                .HasPrecision(10, 2)
                .HasConversion(
                    preco => preco.Value,
                    value => PrecoProduto.Reconstituir(value))
                .IsRequired();

            entity.Property(p => p.Categoria)
                .IsRequired()
                .HasConversion(
                    categoria => categoria.Value,
                    value => CategoriaProduto.Reconstituir(value))
                .HasMaxLength(50);

            entity.Property(p => p.Estoque)
                .HasConversion(
                    estoque => estoque.Value,
                    value => EstoqueProduto.Reconstituir(value))
                .IsRequired();

            entity.Property(p => p.ContatoEmail)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Ativo)
                .HasDefaultValue(true);

            // Permite EF Core ler/escrever em propriedades com private setters
            entity.Property(p => p.Nome).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(p => p.Descricao).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(p => p.Preco).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(p => p.Categoria).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(p => p.Estoque).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(p => p.Ativo).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(p => p.ContatoEmail).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(p => p.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(p => p.DataAtualizacao).UsePropertyAccessMode(PropertyAccessMode.Property);
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nome).IsRequired().HasMaxLength(100).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(c => c.Slug).IsRequired().HasMaxLength(120).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(c => c.CategoriaPaiId).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(c => c.Ativa).HasDefaultValue(true).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(c => c.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(c => c.DataAtualizacao).UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("idx_categoria_slug");

            entity.HasOne<Categoria>()
                .WithMany()
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

            entity.Property(v => v.Sku)
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion(sku => sku.Valor, valor => SKU.Reconstituir(valor))
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.HasIndex(v => new { v.ProdutoId, v.Sku })
                .IsUnique()
                .HasDatabaseName("idx_variante_produto_sku");

            entity.Property(v => v.Descricao).IsRequired().HasMaxLength(200)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(v => v.PrecoAdicional)
                .HasPrecision(10, 2)
                .HasConversion(p => p.Value, v => PrecoProduto.Reconstituir(v))
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(v => v.Estoque)
                .HasConversion(e => e.Value, v => EstoqueProduto.Reconstituir(v))
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(v => v.Ativa).HasDefaultValue(true).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(v => v.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
            entity.Property(v => v.DataAtualizacao).UsePropertyAccessMode(PropertyAccessMode.Property);
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Status)
                .HasConversion<string>()
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.Total)
                .HasPrecision(10, 2)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.CriadoEm)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.ConfirmadoEm)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.CanceladoEm)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(p => p.MotivoCancelamento)
                .HasMaxLength(500)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.HasMany(p => p.Itens)
                .WithOne()
                .HasForeignKey(i => i.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PedidoItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.NomeProduto)
                .IsRequired()
                .HasMaxLength(100)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(i => i.PrecoUnitario)
                .HasPrecision(10, 2)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(i => i.Quantidade)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Property(i => i.ProdutoId)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            entity.Ignore(i => i.Subtotal);
        });
    }
}
