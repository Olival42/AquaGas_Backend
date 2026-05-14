using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Sale.Application.UseCases;

public interface IGetAllSales
{
    Task<Result<List<SaleResponse>>> Execute();
}