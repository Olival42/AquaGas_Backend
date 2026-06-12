using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Shared;

[Collection("Integration")]
public class ValidationFilterTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ValidationFilterTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithEmptyBody_ReturnsBadRequest()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterProduct_WithMissingFields_ReturnsBadRequest()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCustomer_WithInvalidEmail_ReturnsBadRequest()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Teste Validação",
            Document = "11144477735",
            Email = "invalido",
            Phone = "11999999999",
            Address = new
            {
                Street = "Rua",
                Neighborhood = "Bairro",
                Number = "1",
                City = "SP",
                Cep = "01001000"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterEmployee_WithShortPassword_ReturnsBadRequest()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "validuser", Password = "123", Role = "Employee" },
            Employee = new
            {
                Name = "Teste Senha Curta",
                Cpf = "91234567890",
                Email = "curta@empresa.com",
                Phone = "11988887777"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterSale_WithEmptyItems_ReturnsBadRequest()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
