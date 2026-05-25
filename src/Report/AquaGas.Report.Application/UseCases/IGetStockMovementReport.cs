using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Report.Application.UseCases;

public interface IGetStockMovementReport
{
    Task<Result<StockMovementReportResponse>> Execute(
        StockMovementReportInput input);
}
