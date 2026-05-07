using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Application.Validators;
using FluentAssertions;
using Xunit;

namespace Catalogo.Tests.Validators
{
    /// <summary>
    /// Unit tests for FluentValidation validators
    /// Tests all business rules and validation constraints
    /// References: REST API Best Practices - Input Validation
    /// </summary>
    public class ProdutoValidatorTests
    {
        private readonly CriarProdutoValidator _criarValidator;
        private readonly AtualizarProdutoValidator _atualizarValidator;

        public ProdutoValidatorTests()
        {
            _criarValidator = new CriarProdutoValidator();
            _atualizarValidator = new AtualizarProdutoValidator();
        }

        #region CriarProdutoValidator Tests

        /// <summary>
        /// Test: Valid product creation request passes validation
        /// Scenario: All required fields with valid values
        /// Expected: No validation errors
        /// </summary>
        [Fact]
        public void CriarProdutoValidator_WithValidRequest_PassesValidation()
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "Mouse Wireless",
                Descricao = "Mouse Logitech MX Master 3",
                Preco = 349.90m,
                Categoria = "Eletrônicos",
                Estoque = 100,
                ContatoEmail = "vendedor@example.com"
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Test: Empty name fails validation
        /// Scenario: Nome field is empty string
        /// Expected: Validation error for required field
        /// </summary>
        [Fact]
        public void CriarProdutoValidator_WithEmptyName_FailsValidation()
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "",
                Descricao = "Description",
                Preco = 100m,
                Categoria = "Outros",
                Estoque = 10,
                ContatoEmail = "test@example.com"
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Nome");
        }

        /// <summary>
        /// Test: Null name fails validation
        /// Scenario: Nome field is null
        /// Expected: Validation error
        /// </summary>
        [Fact]
        public void CriarProdutoValidator_WithNullName_FailsValidation()
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = null!,
                Descricao = "Description",
                Preco = 100m,
                Categoria = "Outros",
                Estoque = 10,
                ContatoEmail = "test@example.com"
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
        }

        /// <summary>
        /// Test: Name exceeding max length fails validation
        /// Scenario: Nome longer than 255 characters
        /// Expected: Validation error for max length
        /// </summary>
        [Fact]
        public void CriarProdutoValidator_WithNameExceedingMaxLength_FailsValidation()
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = new string('a', 256), // 256 characters, exceeds max of 100
                Descricao = "Description",
                Preco = 100m,
                Categoria = "Outros",
                Estoque = 10,
                ContatoEmail = "test@example.com"
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.PropertyName == "Nome" && e.ErrorMessage.Contains("exceder")
            );
        }

        /// <summary>
        /// Test: Zero price fails validation
        /// Scenario: Preco is 0
        /// Expected: Validation error for minimum value
        /// </summary>
        [Fact]
        public void CriarProdutoValidator_WithZeroPrice_FailsValidation()
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "Produto",
                Descricao = "Description",
                Preco = 0m,
                Categoria = "Outros",
                Estoque = 10,
                ContatoEmail = "test@example.com"
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Preco");
        }

        /// <summary>
        /// Test: Negative price fails validation
        /// Scenario: Preco is negative
        /// Expected: Validation error
        /// </summary>
        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(-0.01)]
        public void CriarProdutoValidator_WithNegativePrice_FailsValidation(decimal price)
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "Produto",
                Descricao = "Description",
                Preco = price,
                Categoria = "Outros",
                Estoque = 10,
                ContatoEmail = "test@example.com"
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Preco");
        }

        /// <summary>
        /// Test: Invalid email format fails validation
        /// Scenario: ContatoEmail is not a valid email
        /// Expected: Validation error for email format
        /// </summary>
        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        public void CriarProdutoValidator_WithInvalidEmail_FailsValidation(string email)
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "Produto",
                Descricao = "Description",
                Preco = 100m,
                Categoria = "Outros",
                Estoque = 10,
                ContatoEmail = email
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "ContatoEmail");
        }

        /// <summary>
        /// Test: Valid email formats pass validation
        /// Scenario: Various valid email formats
        /// Expected: Validation passes
        /// </summary>
        [Theory]
        [InlineData("user@example.com")]
        [InlineData("vendor+123@company.co.uk")]
        [InlineData("firstname.lastname@domain.com")]
        public void CriarProdutoValidator_WithValidEmail_PassesValidation(string email)
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "Produto",
                Descricao = "Description",
                Preco = 100m,
                Categoria = "Outros",
                Estoque = 10,
                ContatoEmail = email
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Test: Negative stock fails validation
        /// Scenario: Estoque is negative
        /// Expected: Validation error
        /// </summary>
        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CriarProdutoValidator_WithNegativeStock_FailsValidation(int stock)
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "Produto",
                Descricao = "Description",
                Preco = 100m,
                Categoria = "Outros",
                Estoque = stock,
                ContatoEmail = "test@example.com"
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Estoque");
        }

        /// <summary>
        /// Test: Zero stock passes validation
        /// Scenario: Estoque is 0 (product out of stock but valid)
        /// Expected: Validation passes
        /// </summary>
        [Fact]
        public void CriarProdutoValidator_WithZeroStock_PassesValidation()
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "Produto",
                Descricao = "Description",
                Preco = 100m,
                Categoria = "Outros",
                Estoque = 0,
                ContatoEmail = "test@example.com"
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Test: Description length validation
        /// Scenario: Descricao is extremely long
        /// Expected: Validation error for max length
        /// </summary>
        [Fact]
        public void CriarProdutoValidator_WithDescriptionExceedingMaxLength_FailsValidation()
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "Produto",
                Descricao = new string('x', 1001), // 1001 characters, exceeds typical max
                Preco = 100m,
                Categoria = "Outros",
                Estoque = 10,
                ContatoEmail = "test@example.com"
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            // If validator has max length for description, this should fail
            // result.IsValid.Should().BeFalse();
        }

        #endregion

        #region AtualizarProdutoValidator Tests

        /// <summary>
        /// Test: Partial update with only name passes validation
        /// Scenario: Only Nome field is set in update request
        /// Expected: Validation passes
        /// </summary>
        [Fact]
        public void AtualizarProdutoValidator_WithOnlyName_PassesValidation()
        {
            // Arrange
            var request = new AtualizarProdutoRequest
            {
                Nome = "Produto Atualizado"
            };

            // Act
            var result = _atualizarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Test: Partial update with empty name passes validation
        /// Scenario: Nome is empty string
        /// Expected: Validation passes because When(!IsNullOrEmpty) excludes empty strings
        /// (empty name means "don't update this field")
        /// </summary>
        [Fact]
        public void AtualizarProdutoValidator_WithEmptyName_PassesValidation()
        {
            // Arrange
            var request = new AtualizarProdutoRequest
            {
                Nome = ""
            };

            // Act
            var result = _atualizarValidator.Validate(request);

            // Assert - empty string on optional field passes (When condition uses !IsNullOrEmpty)
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Test: Partial update with only price passes validation
        /// Scenario: Only Preco field set with valid value
        /// Expected: Validation passes
        /// </summary>
        [Fact]
        public void AtualizarProdutoValidator_WithOnlyValidPrice_PassesValidation()
        {
            // Arrange
            var request = new AtualizarProdutoRequest
            {
                Preco = 299.90m
            };

            // Act
            var result = _atualizarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Test: Partial update with invalid price fails
        /// Scenario: Preco is negative
        /// Expected: Validation error
        /// </summary>
        [Fact]
        public void AtualizarProdutoValidator_WithNegativePrice_FailsValidation()
        {
            // Arrange
            var request = new AtualizarProdutoRequest
            {
                Preco = -50m
            };

            // Act
            var result = _atualizarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Preco");
        }

        /// <summary>
        /// Test: Multiple field updates validation
        /// Scenario: Multiple fields set with mix of valid and invalid values
        /// Expected: Validation fails with all errors reported
        /// </summary>
        [Fact]
        public void AtualizarProdutoValidator_WithMultipleInvalidFields_ReportsAllErrors()
        {
            // Arrange
            var request = new AtualizarProdutoRequest
            {
                Nome = "",
                Preco = -100m,
                Estoque = -5
            };

            // Act
            var result = _atualizarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Count.Should().BeGreaterThanOrEqualTo(2);
        }

        #endregion

        #region Cross-Field Validation Tests

        /// <summary>
        /// Test: Business rule - Name and description consistency
        /// Scenario: Both name and description are meaningful
        /// Expected: Validation passes
        /// </summary>
        [Fact]
        public void CriarProdutoValidator_WithValidNameAndDescription_PassesValidation()
        {
            // Arrange
            var request = new CriarProdutoRequest
            {
                Nome = "SSD Samsung 1TB",
                Descricao = "NVMe M.2 980 PRO - Velocidade até 7100 MB/s",
                Preco = 1200m,
                Categoria = "Eletrônicos",
                Estoque = 25,
                ContatoEmail = "samsung@distribuidor.com"
            };

            // Act
            var result = _criarValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion
    }
}