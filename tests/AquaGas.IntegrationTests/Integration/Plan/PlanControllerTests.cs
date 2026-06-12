using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Plan;

[Collection("Integration")]
public class PlanControllerTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PlanControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<string> CreateProductAsync(HttpClient client, string name, int qty = 100)
    {
        var response = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = name,
            Type = "Gas",
            Price = 89.90m,
            Quantity = qty
        });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("data").GetProperty("id").GetString()!;
    }

    private async Task<string> CreateCustomerAsync(
        HttpClient client, string cpf, string name, string email)
    {
        var response = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = name,
            Document = cpf,
            Email = email,
            Phone = "11999990001",
            Address = new
            {
                Street = "Rua Teste",
                Neighborhood = "Centro",
                Number = "100",
                City = "São Paulo",
                Cep = "01001000"
            }
        });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);
        var productId = await CreateProductAsync(client, "Plano Prod 01");
        var customerId = await CreateCustomerAsync(
            client, "52998224725", "Cliente Plano 01", "plano01@email.com");

        var response = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = customerId,
            Cycle = "Monthly",
            DeliveryDay = 10,
            BillingDay = 5,
            IgnoreWarnings = true,
            Items = new[]
            {
                new { ProductId = productId, Quantity = 2 }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Register_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = Guid.NewGuid().ToString(),
            Cycle = "Monthly",
            DeliveryDay = 10,
            BillingDay = 5,
            Items = new[]
            {
                new { ProductId = Guid.NewGuid().ToString(), Quantity = 1 }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNotFound()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.GetAsync($"/api/plans/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.GetAsync("/api/plans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Register_GetById_ReturnsRegisteredPlan()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);
        var productId = await CreateProductAsync(client, "Plano Prod GetById");
        var customerId = await CreateCustomerAsync(
            client, "71461516030", "Cliente Plano GetById", "planoget@email.com");

        var registerResponse = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = customerId,
            Cycle = "Monthly",
            DeliveryDay = 15,
            BillingDay = 10,
            IgnoreWarnings = true,
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1 }
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var planId = json.GetProperty("data").GetProperty("id").GetString();

        var getResponse = await client.GetAsync($"/api/plans/{planId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getJson.GetProperty("data").GetProperty("id").GetString().Should().Be(planId);
    }

    [Fact]
    public async Task Suspend_ActivePlan_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);
        var productId = await CreateProductAsync(client, "Plano Prod Suspend");
        var customerId = await CreateCustomerAsync(
            client, "52998224997", "Cliente Plano Suspend", "planosuspend@email.com");

        var registerResponse = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = customerId,
            Cycle = "Monthly",
            DeliveryDay = 20,
            BillingDay = 15,
            IgnoreWarnings = true,
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1 }
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var planId = json.GetProperty("data").GetProperty("id").GetString();

        var suspendContent = JsonContent.Create(new { Reason = "Teste suspensão" });
        var suspendResponse = await client.PatchAsync($"/api/plans/{planId}/suspend", suspendContent);

        suspendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var suspendJson = await suspendResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        suspendJson.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_ActivePlan_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);
        var productId = await CreateProductAsync(client, "Plano Prod Cancel");
        var customerId = await CreateCustomerAsync(
            client, "98765432100", "Cliente Plano Cancel", "planocancel@email.com");

        var registerResponse = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = customerId,
            Cycle = "Monthly",
            DeliveryDay = 25,
            BillingDay = 20,
            IgnoreWarnings = true,
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1 }
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var planId = json.GetProperty("data").GetProperty("id").GetString();

        var cancelContent = JsonContent.Create(new { Reason = "Teste cancelamento" });
        var cancelResponse = await client.PatchAsync($"/api/plans/{planId}/cancel", cancelContent);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelJson = await cancelResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        cancelJson.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
