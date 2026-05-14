using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Sale.Application.UseCases;

public interface IRegisterSale
{
    Task<Result<SaleResponse>> Execute(RegisterSaleInput input);
}
