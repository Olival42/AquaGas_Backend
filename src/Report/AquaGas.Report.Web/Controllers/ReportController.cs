using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.UseCases;
using AquaGas.Shared.Http;
using AquaGas.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaGas.Report.Web.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportController : ControllerBase
{
    private readonly IGetStockMovementReport _getStockMovementReport;
    private readonly IGetSalesReport _getSalesReport;
    private readonly IGetContractPenaltyReport _getContractPenaltyReport;

    public ReportController(
        IGetStockMovementReport getStockMovementReport,
        IGetSalesReport getSalesReport,
        IGetContractPenaltyReport getContractPenaltyReport)
    {
        _getStockMovementReport = getStockMovementReport;
        _getSalesReport = getSalesReport;
        _getContractPenaltyReport = getContractPenaltyReport;
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("stock-movements")]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] StockMovementReportInput input)
    {
        var result = await _getStockMovementReport.Execute(input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("sales")]
    public async Task<IActionResult> GetSales(
        [FromQuery] SalesReportInput input)
    {
        var result = await _getSalesReport.Execute(input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("contract-penalties")]
    public async Task<IActionResult> GetContractPenalties(
        [FromQuery] ContractPenaltyReportInput input)
    {
        var result = await _getContractPenaltyReport.Execute(input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }
}
