using AquaGas.Customer.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Sale.Application.Dtos.Responses;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Sale.Application.UseCases;

public class GetById : IGetById
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IProductRepository _productRepository;

    public GetById(
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        IEmployeeRepository employeeRepository,
        IProductRepository productRepository)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _employeeRepository = employeeRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<SaleResponse>> Execute(Guid id)
    {
        var sale = await _saleRepository.GetByIdWithItemsAsync(id);
        
        if (sale is null)
            return Result<SaleResponse>.Fail(
                Error.NotFound("Sale not found"));

        CustomerSaleResponse? customerResponse = null;
        if (sale.CustomerId.HasValue)
        {
            var customer =
                await _customerRepository.GetByIdAsync(
                    sale.CustomerId.Value, false);

            if (customer is not null)
            {
                customerResponse = new CustomerSaleResponse
                {
                    Id = customer.Id,
                    Name = customer.Name.Value,
                    Document = customer.Document.Value
                };
            }
        }

        var employee =
            await _employeeRepository.GetByIdAsync(
                sale.EmployeeId, false);

        if (employee is null)
            return Result<SaleResponse>.Fail(
                Error.NotFound("Employee not found"));

        var itemResponses = new List<SaleItemResponse>();
        foreach (var item in sale.Items)
        {
            var product =
                await _productRepository.GetByIdAsync(
                    item.ProductId, false);

            itemResponses.Add(new SaleItemResponse
            {
                ProductId = item.ProductId,
                ProductName = product?.Name.Value ?? "Unknown",
                Quantity = item.Quantity.Value,
                Total = item.TotalPrice.Value
            });
        }

        var subtotal = sale.Items.Sum(x => x.TotalPrice.Value);
        var discount = sale.CurrentDiscount?.Value ?? 0;

        var response = new SaleResponse
        {
            Id = sale.Id,
            Customer = customerResponse,
            Employee = new EmployeeSaleResponse
            {
                Id = employee.Id,
                Name = employee.Name.Value
            },
            Items = itemResponses,
            Subtotal = subtotal,
            Discount = discount,
            Total = sale.Total.Value,
            Status = sale.Status,
            CancelReason = sale.CancelReason!,
            CreatedAt = sale.Date
        };

        return Result<SaleResponse>.Success(response);
    }
}