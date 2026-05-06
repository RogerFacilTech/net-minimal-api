using AutoMapper;
using Catalogo.Domain;
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Catalogo.Tests.Services
{
    public class ProdutoServiceTests
    {
        private readonly Mock<IProdutoQueryRepository> _mockQueryRepository;
        private readonly Mock<IProdutoCommandRepository> _mockCommandRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<ProdutoService>> _mockLogger;
        private readonly ProdutoService _service;

        public ProdutoServiceTests()
        {
            _mockQueryRepository = new Mock<IProdutoQueryRepository>();
            _mockCommandRepository = new Mock<IProdutoCommandRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<ProdutoService>>();

            _service = new ProdutoService(
                _mockQueryRepository.Object,
                _mockCommandRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        #region ListarProdutosAsync Tests

        [Fact]
        public async Task ListarProdutosAsync_WithValidPagination_ReturnsPaginatedProducts()
        {
            // Arrange
            var responses = CreateMockProdutoResponses(10);

            _mockQueryRepository
                .Setup(r => r.ListarAsync(1, 10, null, null))
                .ReturnsAsync(((IReadOnlyList<ProdutoResponse>)responses, 10));

            // Act
            var result = await _service.ListarProdutosAsync(1, 10);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(10);
            result.Pagination.Page.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task ListarProdutosAsync_WithInvalidPageNumber_NormalizesToPage1(int pageNumber)
        {
            // Arrange
            _mockQueryRepository
                .Setup(r => r.ListarAsync(1, 10, null, null))
                .ReturnsAsync(((IReadOnlyList<ProdutoResponse>)new List<ProdutoResponse>(), 0));

            // Act
            var result = await _service.ListarProdutosAsync(pageNumber, 10);

            // Assert
            result.Should().NotBeNull();
            result.Pagination.Page.Should().Be(1);
        }

        [Fact]
        public async Task ListarProdutosAsync_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            _mockQueryRepository
                .Setup(r => r.ListarAsync(1, 10, null, null))
                .ReturnsAsync(((IReadOnlyList<ProdutoResponse>)new List<ProdutoResponse>(), 0));

            // Act
            var result = await _service.ListarProdutosAsync(1, 10);

            // Assert
            result.Data.Should().BeEmpty();
            result.Pagination.TotalItems.Should().Be(0);
        }

        #endregion

        #region ObterProdutoAsync Tests

        [Fact]
        public async Task ObterProdutoAsync_WithValidId_ReturnsProduto()
        {
            // Arrange
            var response = new ProdutoResponse { Id = 1, Nome = "Notebook Dell", Preco = 5500.00m, Estoque = 10 };

            _mockQueryRepository.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(response);

            // Act
            var result = await _service.ObterProdutoAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Nome.Should().Be("Notebook Dell");
            result.Preco.Should().Be(5500.00m);
        }

        [Fact]
        public async Task ObterProdutoAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockQueryRepository.Setup(r => r.ObterPorIdAsync(999)).ReturnsAsync((ProdutoResponse?)null);

            // Act
            var result = await _service.ObterProdutoAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ObterProdutoAsync_WithInactiveProduct_ReturnsNull()
        {
            // Arrange — repository already filters inactive; returns null
            _mockQueryRepository.Setup(r => r.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync((ProdutoResponse?)null);

            // Act
            var result = await _service.ObterProdutoAsync(1);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CriarProdutoAsync Tests

        [Fact]
        public async Task CriarProdutoAsync_WithValidRequest_CreatesProduto()
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "Mouse Logitech",
                Descricao = "Wireless USB",
                Preco = 150.00m,
                Categoria = "Eletrônicos",
                Estoque = 50,
                ContatoEmail = "vendor@example.com"
            };

            var produto = Produto.Criar(request.Nome, request.Descricao, request.Preco, request.Categoria, request.Estoque, request.ContatoEmail).Value!;
            var response = new ProdutoResponse { Id = 1, Nome = request.Nome, Preco = request.Preco, Estoque = request.Estoque };

            _mockCommandRepository.Setup(r => r.AdicionarAsync(It.IsAny<Produto>())).ReturnsAsync(produto);
            _mockMapper.Setup(m => m.Map<ProdutoResponse>(It.IsAny<Produto>())).Returns(response);

            // Act
            var result = await _service.CriarProdutoAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Nome.Should().Be("Mouse Logitech");
        }

        #endregion

        #region AtualizarProdutoAsync Tests

        [Fact]
        public async Task AtualizarProdutoAsync_WithPartialUpdate_UpdatesProduct()
        {
            // Arrange
            var produto = Produto.Criar("Notebook Original", "Descricao do notebook original", 5000m, "Eletrônicos", 5, "test@example.com").Value!;
            var request = new AtualizarProdutoRequest { Nome = "Notebook Atualizado" };
            var response = new ProdutoResponse { Nome = "Notebook Atualizado", Preco = 5000m };

            _mockCommandRepository.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(produto);
            _mockCommandRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<ProdutoResponse>(produto)).Returns(response);

            // Act
            var result = await _service.AtualizarProdutoAsync(1, request);

            // Assert
            result.Should().NotBeNull();
            result!.Nome.Should().Be("Notebook Atualizado");
        }

        [Fact]
        public async Task AtualizarProdutoAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockCommandRepository.Setup(r => r.ObterPorIdAsync(999)).ReturnsAsync((Produto?)null);

            // Act
            var result = await _service.AtualizarProdutoAsync(999, new AtualizarProdutoRequest { Nome = "Test" });

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region AtualizarCompletoProdutoAsync Tests

        [Fact]
        public async Task AtualizarCompletoProdutoAsync_WithValidRequest_ReplacesAllFields()
        {
            // Arrange
            var produto = Produto.Criar("Mouse Antigo", "Wireless USB preciso", 150m, "Eletrônicos", 50, "old@example.com").Value!;
            var request = new CriarProdutoRequest
            {
                Nome = "Mouse Atualizado",
                Descricao = "Wireless + Bluetooth",
                Preco = 200m,
                Categoria = "Eletrônicos",
                Estoque = 75,
                ContatoEmail = "newvendor@example.com"
            };
            var response = new ProdutoResponse { Nome = request.Nome, Preco = request.Preco, Estoque = request.Estoque };

            _mockCommandRepository.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(produto);
            _mockCommandRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<ProdutoResponse>(produto)).Returns(response);

            // Act
            var result = await _service.AtualizarCompletoProdutoAsync(1, request);

            // Assert
            result.Should().NotBeNull();
            result!.Nome.Should().Be("Mouse Atualizado");
            result.Preco.Should().Be(200m);
        }

        #endregion

        #region DeletarProdutoAsync Tests

        [Fact]
        public async Task DeletarProdutoAsync_WithValidId_PerformsSoftDelete()
        {
            // Arrange
            _mockCommandRepository.Setup(r => r.DeletarAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _service.DeletarProdutoAsync(1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeletarProdutoAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            _mockCommandRepository.Setup(r => r.DeletarAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _service.DeletarProdutoAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeletarProdutoAsync_WithAlreadyDeletedProduct_ReturnsFalse()
        {
            // Arrange — repository returns false for inactive products
            _mockCommandRepository.Setup(r => r.DeletarAsync(It.IsAny<int>())).ReturnsAsync(false);

            // Act
            var result = await _service.DeletarProdutoAsync(1);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Helper Methods

        private List<Produto> CreateMockProdutos(int count)
        {
            var produtos = new List<Produto>();
            for (int i = 1; i <= count; i++)
            {
                var p = Produto.Criar($"Produto {i}", $"Descrição {i}", 100m * i, "Eletrônicos", 10 * i, $"vendor{i}@example.com").Value!;
                produtos.Add(p);
            }
            return produtos;
        }

        private List<ProdutoResponse> CreateMockProdutoResponses(int count)
        {
            var responses = new List<ProdutoResponse>();
            for (int i = 1; i <= count; i++)
            {
                responses.Add(new ProdutoResponse
                {
                    Id = i,
                    Nome = $"Produto {i}",
                    Descricao = $"Descrição {i}",
                    Preco = 100m * i,
                    Categoria = "Eletrônicos",
                    Estoque = 10 * i
                });
            }
            return responses;
        }

        #endregion
    }
}
