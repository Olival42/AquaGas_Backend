using AquaGas.Customer.Application.UseCases;
using AquaGas.Customer.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

public class ExistsCustomerByDocumentTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly ExistsCustomerByDocument _useCase;

    public ExistsCustomerByDocumentTests()
    {
        _repositoryMock = new Mock<ICustomerRepository>();
        _useCase = new ExistsCustomerByDocument(_repositoryMock.Object);
    }

    [Fact]
    public async Task Should_Return_True_When_Document_Exists()
    {
        _repositoryMock.Setup(x => x.AnyByDocumentAsync("55964416004"))
            .ReturnsAsync(true);

        var result = await _useCase.Execute("559.644.160-04");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _repositoryMock.Verify(x => x.AnyByDocumentAsync("55964416004"), Times.Once);
    }

    [Fact]
    public async Task Should_Return_True_When_CPF_Exists_With_Mask()
    {
        _repositoryMock
            .Setup(x => x.AnyByDocumentAsync("55964416004"))
            .ReturnsAsync(true);

        var result = await _useCase.Execute("559.644.160-04");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _repositoryMock.Verify(
            x => x.AnyByDocumentAsync("55964416004"),
            Times.Once);
    }

    [Fact]
    public async Task Should_Return_True_When_Cnpj_Exists()
    {
        _repositoryMock.Setup(x => x.AnyByDocumentAsync("11222333000181"))
            .ReturnsAsync(true);

        var result = await _useCase.Execute("11.222.333/0001-81");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _repositoryMock.Verify(x => x.AnyByDocumentAsync("11222333000181"), Times.Once);
    }

    [Fact]
    public async Task Should_Return_True_When_CNPJ_Exists_With_Mask()
    {
        _repositoryMock
            .Setup(x => x.AnyByDocumentAsync("11222333000181"))
            .ReturnsAsync(true);

        var result = await _useCase.Execute("11.222.333/0001-81");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _repositoryMock.Verify(
            x => x.AnyByDocumentAsync("11222333000181"),
            Times.Once);
    }
    [Fact]
    public async Task Should_Fail_When_Document_Is_Null()
    {
        var result = await _useCase.Execute(null!);

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(
            x => x.AnyByDocumentAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Fail_When_Document_Is_Empty()
    {
        var result = await _useCase.Execute("");

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(
            x => x.AnyByDocumentAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Fail_When_Document_Is_Whitespace()
    {
        var result = await _useCase.Execute("   ");

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(
            x => x.AnyByDocumentAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Normalize_Document_Before_Query()
    {
        _repositoryMock
            .Setup(x => x.AnyByDocumentAsync("55964416004"))
            .ReturnsAsync(true);

        await _useCase.Execute("559.644.160-04");

        _repositoryMock.Verify(
            x => x.AnyByDocumentAsync("55964416004"),
            Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Document_Is_Invalid_Format()
    {
        var result = await _useCase.Execute("123");

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(
            x => x.AnyByDocumentAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_False_When_Document_Does_Not_Exist()
    {
        _repositoryMock.Setup(x => x.AnyByDocumentAsync("55964416004"))
            .ReturnsAsync(false);

        var result = await _useCase.Execute("55964416004");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Return_Failure_When_Document_Is_Invalid()
    {
        var result = await _useCase.Execute("123");

        result.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(x => x.AnyByDocumentAsync(It.IsAny<string>()), Times.Never);
    }
}
