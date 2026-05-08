namespace AquaGas.API.Modules.Product.Application.Repositories;

using AquaGas.Api.Modules.Product.Domain.Models;

public interface IStockMovementRepository
{
    Task AddAsync(StockMovement log);
}