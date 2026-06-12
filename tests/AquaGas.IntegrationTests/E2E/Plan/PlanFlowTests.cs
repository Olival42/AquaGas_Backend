using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.E2E.Plan;

[Collection("Integration")]
public class PlanFlowTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PlanFlowTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CompletePlanFlow_Register_Verify_Suspend_Reactivate()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var productResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Botijão E2E Plan",
            Type = "Gas",
            Price = 90.00m,
            Quantity = 200
        });
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var productJson = await productResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = productJson.GetProperty("data").GetProperty("id").GetString()!;

        var customerResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Cliente E2E Plan",
            Document = "52998225292",
            Email = "e2e.plan@email.com",
            Phone = "11999992000",
            Address = new
            {
                Street = "Rua E2E Plan",
                Neighborhood = "Centro",
                Number = "100",
                City = "São Paulo",
                Cep = "01001000"
            }
        });
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var customerJson = await customerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var customerId = customerJson.GetProperty("data").GetProperty("id").GetString()!;

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
        var planId = planJson.GetProperty("data").GetProperty("id").GetString()!;

        var getResponse = await client.GetAsync($"/api/plans/{planId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getJson.GetProperty("data").GetProperty("status").GetString().Should().Be("Active");

        var suspendContent = JsonContent.Create(new { Reason = "Suspensão E2E" });
        var suspendResponse = await client.PatchAsync($"/api/plans/{planId}/suspend", suspendContent);
        suspendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterSuspend = await client.GetAsync($"/api/plans/{planId}");
        var afterSuspendJson = await afterSuspend.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        afterSuspendJson.GetProperty("data").GetProperty("status").GetString().Should().Be("Suspended");

        var reactivateResponse = await client.PatchAsync($"/api/plans/{planId}/reactivate", null);
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterReactivate = await client.GetAsync($"/api/plans/{planId}");
        var afterReactivateJson = await afterReactivate.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        afterReactivateJson.GetProperty("data").GetProperty("status").GetString().Should().Be("Active");
    }

    [Fact]
    public async Task PlanCancelFlow_Register_Cancel_VerifyStatus()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var productResponse = await client.PostAsJsonAsync("/api/products/register", new
        { Name = "Botijao E2E Cancel", Type = "Gas", Price = 80.00m, Quantity = 100 });
        productResponse.EnsureSuccessStatusCode();
        var productJson = await productResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = productJson.GetProperty("data").GetProperty("id").GetString()!;

        var customerResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Cliente E2E Cancel Plan", Document = "52998225373",
            Email = "e2e.cancelplan@email.com", Phone = "11999993000",
            Address = new { Street = "Rua Cancel", Neighborhood = "Centro", Number = "200", City = "Sao Paulo", Cep = "01001000" }
        });
        customerResponse.EnsureSuccessStatusCode();
        var customerJson = await customerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var customerId = customerJson.GetProperty("data").GetProperty("id").GetString()!;

        var planResponse = await client.PostAsJsonAsync("/api/plans/register", new
        {
            CustomerId = customerId, Cycle = "Custom", DeliveryDay = 15, BillingDay = 10,
            DurationInMonths = 12, IgnoreWarnings = true,
            Items = new[] { new { ProductId = productId, Quantity = 1 } }
        });
        planResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var planJson = await planResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var planId = planJson.GetProperty("data").GetProperty("id").GetString()!;

        var cancelContent = JsonContent.Create(new { Reason = "Cancelamento E2E contrato" });
        var cancelResponse = await client.PatchAsync($"/api/plans/{planId}/cancel", cancelContent);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterCancel = await client.GetAsync($"/api/plans/{planId}");
        var afterCancelJson = await afterCancel.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        afterCancelJson.GetProperty("data").GetProperty("status").GetString().Should().Be("Canceled");
    }
}
