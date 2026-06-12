using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.Dtos.Responses;
using AquaGas.Report.Application.UseCases;
using AquaGas.Shared.Http;
using AquaGas.Shared.OpenApi;
using AquaGas.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaGas.Report.Web.Controllers;

/// <summary>
/// Relatórios gerenciais de movimentação de estoque, vendas e penalidades contratuais.
/// </summary>
[ApiController]
[Route("api/reports")]
[Produces("application/json")]
[Tags(ApiDocumentation.Tags.Reports)]
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

    /// <summary>
    /// Relatório de movimentações de estoque no período.
    /// </summary>
    /// <remarks>Requer perfil <c>Manager</c>.</remarks>
    /// <param name="input">Período de consulta (data inicial e final).</param>
    /// <response code="200">Relatório gerado com sucesso.</response>
    /// <response code="400">Período inválido.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (apenas Manager).</response>
    [Authorize(Roles = "Manager")]
    [HttpGet("stock-movements")]
    [ProducesResponseType(typeof(ApiResponse<StockMovementReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
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

    /// <summary>
    /// Relatório consolidado de vendas no período.
    /// </summary>
    /// <remarks>Requer perfil <c>Manager</c>.</remarks>
    /// <param name="input">Período de consulta (data inicial e final).</param>
    /// <response code="200">Relatório gerado com sucesso.</response>
    /// <response code="400">Período inválido.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (apenas Manager).</response>
    [Authorize(Roles = "Manager")]
    [HttpGet("sales")]
    [ProducesResponseType(typeof(ApiResponse<SalesReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
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

    /// <summary>
    /// Relatório de penalidades contratuais no período.
    /// </summary>
    /// <remarks>Requer perfil <c>Manager</c>.</remarks>
    /// <param name="input">Período de consulta (data inicial e final).</param>
    /// <response code="200">Relatório gerado com sucesso.</response>
    /// <response code="400">Período inválido.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (apenas Manager).</response>
    [Authorize(Roles = "Manager")]
    [HttpGet("contract-penalties")]
    [ProducesResponseType(typeof(ApiResponse<ContractPenaltyReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
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
