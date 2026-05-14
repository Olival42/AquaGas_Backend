using AquaGas.Sale.Domain.Models;
using AquaGas.Shared.Domain.ValueObjects;
using Xunit;

public class SaleTests
{
    [Fact]
    public void Should_Create_Sale_With_Valid_Data()
    {
        var customerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var price = Price.Create(100).Value!;
        var discount = Discount.Create(10).Value;

        var sale = new Sale(
            customerId,
            employeeId,
            price,
            discount);

        Assert.NotEqual(Guid.Empty, sale.Id);
        Assert.Equal(customerId, sale.CustomerId);
        Assert.Equal(employeeId, sale.EmployeeId);
        Assert.Equal(price, sale.Total);
        Assert.Equal(discount, sale.CurrentDiscount);
        Assert.True(sale.Date <= DateTime.UtcNow);
    }

    [Fact]
    public void Should_Create_Sale_Without_Customer()
    {
        var employeeId = Guid.NewGuid();

        var price = Price.Create(100).Value!;

        var sale = new Sale(
            null,
            employeeId,
            price,
            null);

        Assert.Null(sale.CustomerId);
        Assert.Equal(employeeId, sale.EmployeeId);
        Assert.Equal(price, sale.Total);
        Assert.Null(sale.CurrentDiscount);
    }

    [Fact]
    public void Should_Add_Item_To_Sale()
    {
        var sale = CreateSale();

        var item = new SaleItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            StockQuantity.Create(2).Value!,
            Price.Create(100).Value!);

        sale.AddItem(item);

        Assert.Single(sale.Items);
        Assert.Contains(item, sale.Items);
    }

    [Fact]
    public void Should_Add_Multiple_Items()
    {
        var sale = CreateSale();

        var item1 = new SaleItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            StockQuantity.Create(2).Value!,
            Price.Create(100).Value!);

        var item2 = new SaleItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            StockQuantity.Create(2).Value!,
            Price.Create(100).Value!);

        sale.AddItem(item1);
        sale.AddItem(item2);

        Assert.Equal(2, sale.Items.Count);
    }

    [Fact]
    public void Should_Update_Total()
    {
        var sale = CreateSale();

        var newTotal = Price.Create(200).Value!;

        sale.UpdateTotal(newTotal);

        Assert.Equal(newTotal, sale.Total);
    }

    [Fact]
    public void Should_Update_Discount()
    {
        var sale = CreateSale();

        var discount = Discount.Create(15).Value!;

        sale.UpdateDiscount(discount);

        Assert.Equal(discount, sale.CurrentDiscount);
    }

    [Fact]
    public void Should_Generate_Different_Ids_For_Each_Sale()
    {
        var sale1 = CreateSale();
        var sale2 = CreateSale();

        Assert.NotEqual(sale1.Id, sale2.Id);
    }

    private static Sale CreateSale()
    {
        return new Sale(
            null,
            Guid.NewGuid(),
            Price.Create(100).Value!,
            null);
    }

    [Fact]
    public void Should_Start_With_Finished_Status()
    {
        var sale = CreateSale();

        Assert.Equal(
            SaleStatus.Finished,
            sale.Status);
    }

    [Fact]
    public void Should_Cancel_Sale()
    {
        var sale = CreateSale();

        sale.Cancel("Customer canceled");

        Assert.Equal(
            SaleStatus.Canceled,
            sale.Status);

        Assert.Equal(
            "Customer canceled",
            sale.CancelReason);
    }

    [Fact]
    public void Should_Start_Without_Cancel_Reason()
    {
        var sale = CreateSale();

        Assert.Null(sale.CancelReason);
    }

    [Fact]
    public void Should_Replace_Cancel_Reason_When_Canceling_Again()
    {
        var sale = CreateSale();

        sale.Cancel("First reason");

        sale.Cancel("Second reason");

        Assert.Equal(
            SaleStatus.Canceled,
            sale.Status);

        Assert.Equal(
            "Second reason",
            sale.CancelReason);
    }

    [Fact]
    public void Should_Create_Sale_With_Current_Date_When_Date_Is_Null()
    {
        var before = DateTime.UtcNow;

        var sale = new Sale(
            null,
            Guid.NewGuid(),
            Price.Create(100).Value!,
            null);

        var after = DateTime.UtcNow;

        Assert.True(
            sale.Date >= before &&
            sale.Date <= after);
    }

    [Fact]
    public void Should_Create_Sale_With_Custom_Date()
    {
        var customDate =
            new DateTime(
                2025,
                1,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc);

        var sale = new Sale(
            null,
            Guid.NewGuid(),
            Price.Create(100).Value!,
            null,
            customDate);

        Assert.Equal(customDate, sale.Date);
    }

    [Fact]
    public void Should_Keep_Items_ReadOnly()
    {
        var sale = CreateSale();

        var items = sale.Items;

        Assert.IsAssignableFrom<
            IReadOnlyCollection<SaleItem>>(items);
    }

    [Fact]
    public void Should_Update_Discount_Correctly_Multiple_Times()
    {
        var sale = CreateSale();

        var firstDiscount =
            Discount.Create(10).Value!;

        var secondDiscount =
            Discount.Create(20).Value!;

        sale.UpdateDiscount(firstDiscount);

        Assert.Equal(
            firstDiscount,
            sale.CurrentDiscount);

        sale.UpdateDiscount(secondDiscount);

        Assert.Equal(
            secondDiscount,
            sale.CurrentDiscount);
    }

    [Fact]
    public void Should_Update_Total_Correctly_Multiple_Times()
    {
        var sale = CreateSale();

        var firstTotal =
            Price.Create(150).Value!;

        var secondTotal =
            Price.Create(300).Value!;

        sale.UpdateTotal(firstTotal);

        Assert.Equal(
            firstTotal,
            sale.Total);

        sale.UpdateTotal(secondTotal);

        Assert.Equal(
            secondTotal,
            sale.Total);
    }
}