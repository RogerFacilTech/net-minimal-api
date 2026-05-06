using FacShopAPI.Shared.Kernel;
using FluentAssertions;
using Xunit;

namespace Catalogo.Tests.Unit.Common;

public class ResultTests
{
    [Fact]
    public void Result_Ok_IsSuccess_True()
    {
        var result = Result.Ok();
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Result_Fail_IsSuccess_False()
    {
        var result = Result.Fail("erro");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("erro");
    }

    [Fact]
    public void ResultT_Ok_CarregaValor()
    {
        var result = Result<int>.Ok(42);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultT_Fail_NaoCarregaValor()
    {
        var result = Result<int>.Fail("erro");
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(default);
    }
}
