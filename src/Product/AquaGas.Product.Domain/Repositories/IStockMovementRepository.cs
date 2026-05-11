namespace AquaGas.Product.Application.Repositories;

using AquaGas.Product.Domain.Models;

public interface IStockMovementRepository
{
    Task AddAsync(StockMovement log);
}