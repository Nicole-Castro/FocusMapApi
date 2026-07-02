using FocusMapApi.Models;
using FluentAssertions;

namespace FocusMapApi.Tests.Unit.Models;

public class ResponseModelTests
{
    // ── Ok ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Ok_ShouldSetSuccessTrue()
    {
        var result = ResponseModel<string>.Ok("data");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Ok_ShouldSetStatusCode200ByDefault()
    {
        var result = ResponseModel<string>.Ok("data");

        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Ok_ShouldSetProvidedData()
    {
        var result = ResponseModel<string>.Ok("hello");

        result.Data.Should().Be("hello");
    }

    [Fact]
    public void Ok_ShouldSetCustomStatusCode()
    {
        var result = ResponseModel<string>.Ok("data", statusCode: 201);

        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public void Ok_ShouldSetMessage()
    {
        var result = ResponseModel<string>.Ok("data", message: "Criado com sucesso");

        result.Message.Should().Be("Criado com sucesso");
    }

    // ── Fail ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Fail_ShouldSetSuccessFalse()
    {
        var result = ResponseModel<string>.Fail("Erro");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void Fail_ShouldSetStatusCode400ByDefault()
    {
        var result = ResponseModel<string>.Fail("Erro");

        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public void Fail_ShouldSetCustomStatusCode()
    {
        var result = ResponseModel<string>.Fail("Não encontrado", 404);

        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public void Fail_ShouldSetMessage()
    {
        var result = ResponseModel<string>.Fail("Usuário não encontrado", 404);

        result.Message.Should().Be("Usuário não encontrado");
    }

    [Fact]
    public void Fail_DataShouldBeNull()
    {
        var result = ResponseModel<string>.Fail("Erro");

        result.Data.Should().BeNull();
    }
}
