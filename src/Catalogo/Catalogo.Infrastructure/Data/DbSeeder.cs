using Catalogo.Domain;
using ProdutosAPI.Catalogo.Application.Interfaces;

namespace ProdutosAPI.Catalogo.Infrastructure.Data;

public static class DbSeeder
{
    public static void Seed(ICatalogoContext context)
    {
        SeedProdutos(context);
        SeedCategorias(context);
    }

    private static void SeedProdutos(ICatalogoContext context)
    {
        if (context.Produtos.Any()) return;

        var produtos = new[]
        {
            Produto.Criar("Notebook Dell XPS 13", "Notebook de alta performance com processador Intel Core i7, 16GB RAM e 512GB SSD", 4500.00m, "Eletrônicos", 5, "vendas@dell.com").Value!,
            Produto.Criar("Mouse Logitech MX Master 3S", "Mouse wireless de precisão profissional com múltiplos botões e rastreamento avançado", 450.00m, "Eletrônicos", 25, "suporte@logitech.com").Value!,
            Produto.Criar("Teclado Mecânico RGB", "Teclado mecânico com iluminação RGB, switches Cherry MX e design compacto", 350.00m, "Eletrônicos", 15, "contato@keyboards.com.br").Value!,
            Produto.Criar("Clean Code", "Guia prático para escrever código limpo e manutenível. Essencial para todo desenvolvedor", 89.90m, "Livros", 30, "vendas@books.com").Value!,
            Produto.Criar("Design Patterns", "Padrões de design reutilizáveis para desenvolvimento de software. Referência obrigatória", 75.00m, "Livros", 20, "vendas@books.com").Value!,
            Produto.Criar("Camiseta técnica Azul", "Camiseta de poliéster com tecnologia anti-transpiração, disponível em vários tamanhos", 79.90m, "Roupas", 50, "vendas@clothing.com.br").Value!,
            Produto.Criar("Café Gourmet 500g", "Café gourmet especial com grãos selecionados de plantações premium da região", 45.00m, "Alimentos", 100, "vendas@coffee.com.br").Value!,
            Produto.Criar("Monitor LG UltraWide 34\"", "Monitor curvo ultrawide com resolução 3440x1440, ideal para produtividade e games", 1899.00m, "Eletrônicos", 3, "suporte@lg.com.br").Value!
        };

        foreach (var p in produtos) context.AddProduto(p);
        context.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static void SeedCategorias(ICatalogoContext context)
    {
        if (context.Categorias.Any()) return;

        // IDs 1-5 reservados: testes de Categoria criam a partir de ID 6
        var raiz = new[]
        {
            Categoria.Criar("Eletrônicos").Value!,
            Categoria.Criar("Livros").Value!,
            Categoria.Criar("Roupas").Value!,
            Categoria.Criar("Alimentos").Value!,
            Categoria.Criar("Outros").Value!
        };

        foreach (var c in raiz) context.AddCategoria(c);
        context.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
