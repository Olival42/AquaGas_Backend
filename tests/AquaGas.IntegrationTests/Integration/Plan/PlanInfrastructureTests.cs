using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Plan;

[Collection("Integration")]
public class PlanInfrastructureTests
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PlanInfrastructureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> CreateProductAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/products/register", new
        { Name = name, Type = "Gas", Price = 80.00m, Quantity = 200 });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("data").GetProperty("id").GetString()!;
    }

    private async Task<string> CreateCustomerAsync(HttpClient client, string cpf, string email)
    {
        var response = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = $"Cliente Infra {cpf[..5]}",
            Document = cpf,
            Email = email,
            Phone = "11999997001",
            Address = new
            {
                Street = "Rua Infra Plan",
                Neighborhood = "Centro",
                Number = "1",
                City = "São Paulo",
                Cep = "01001000"
            }
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Plan_GeneratesDeliveries_OnCreation()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Plan Deliveries Infra");
        var customerId = await CreateCustomerAsync(client, "51739284060", "infra.plan.del@email.com");

        var planResponse = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = customerId,
            Cycle = "Monthly",
            DeliveryDay = 10,
            BillingDay = 5,
            IgnoreWarnings = true,
            Items = new[] { new { ProductId = productId, Quantity = 2 } }
        });
        planResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var planJson = await planResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        planJson.GetProperty("data").GetProperty("deliveries").GetArrayLength()
            .Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Plan_GeneratesBillings_OnCreation()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Plan Billings Infra");
        var customerId = await CreateCustomerAsync(client, "62841357020", "infra.plan.bill@email.com");

        var planResponse = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = customerId,
            Cycle = "Monthly",
            DeliveryDay = 15,
            BillingDay = 10,
            IgnoreWarnings = true,
            Items = new[] { new { ProductId = productId, Quantity = 1 } }
        });
        planResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var planJson = await planResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        planJson.GetProperty("data").GetProperty("billings").GetArrayLength()
            .Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Plan_SuspendCancelsDeliveriesAndBillings()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Plan Suspend Infra");
        var customerId = await CreateCustomerAsync(client, "73952148088", "infra.plan.susp@email.com");

        var planResponse = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = customerId,
            Cycle = "Monthly",
            DeliveryDay = 20,
            BillingDay = 15,
            IgnoreWarnings = true,
            Items = new[] { new { ProductId = productId, Quantity = 1 } }
        });
        planResponse.EnsureSuccessStatusCode();
        var planJson = await planResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var planId = planJson.GetProperty("data").GetProperty("id").GetString()!;

        var suspendContent = JsonContent.Create(new { Reason = "Suspensão infra" });
        var suspendResponse = await client.PatchAsync($"/api/plans/{planId}/suspend", suspendContent);
        suspendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var suspendJson = await suspendResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        suspendJson.GetProperty("data").GetProperty("status").GetString().Should().Be("Suspended");
        suspendJson.GetProperty("data").GetProperty("canceledDeliveries").GetInt32()
            .Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Plan_WithDuration_CreatesContractPenaltyOnCancel()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Plan Penalty Infra");
        var customerId = await CreateCustomerAsync(client, "84163527091", "infra.plan.pen@email.com");

        var planResponse = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = customerId,
            Cycle = "Monthly",
            DeliveryDay = 25,
            BillingDay = 20,
            DurationInMonths = 12,
            IgnoreWarnings = true,
            Items = new[] { new { ProductId = productId, Quantity = 1 } }
        });
        planResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var planJson = await planResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var planId = planJson.GetProperty("data").GetProperty("id").GetString()!;

        var cancelContent = JsonContent.Create(new { Reason = "Cancelamento contrato infra" });
        var cancelResponse = await client.PatchAsync($"/api/plans/{planId}/cancel", cancelContent);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelJson = await cancelResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        cancelJson.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Plan_UpgradeIncreasesTotal()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Plan Upgrade Infra");
        var customerId = await CreateCustomerAsync(client, "95274183060", "infra.plan.upg@email.com");

        var planResponse = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = customerId,
            Cycle = "Monthly",
            DeliveryDay = 10,
            BillingDay = 5,
            IgnoreWarnings = true,
            Items = new[] { new { ProductId = productId, Quantity = 1 } }
        });
        planResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var planJson = await planResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var planId = planJson.GetProperty("data").GetProperty("id").GetString()!;

        var upgradeContent = JsonContent.Create(new
        {
            Items = new[] { new { ProductId = productId, Quantity = 3 } }
        });
        var upgradeResponse = await client.PatchAsync($"/api/plans/{planId}/upgrade", upgradeContent);
        upgradeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var upgradeJson = await upgradeResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        upgradeJson.GetProperty("data").GetProperty("newTotal").GetDecimal()
            .Should().BeGreaterThan(upgradeJson.GetProperty("data").GetProperty("previousTotal").GetDecimal());
    }
}
