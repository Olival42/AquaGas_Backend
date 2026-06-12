namespace AquaGas.Product.Application.Repositories;

using AquaGas.Product.Domain.Models;
using AquaGas.Product.Domain.Enums;

public interface IStockMovementRepository
{
    Task AddAsync(StockMovement log);
    Task<List<StockMovement>> GetReportAsync(
        DateTime start,
        DateTime end);
}
