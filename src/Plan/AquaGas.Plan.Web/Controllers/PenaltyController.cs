namespace AquaGas.Plan.Web.Controllers;

using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Shared.Http;
using AquaGas.Shared.OpenApi;
using AquaGas.Shared.Responses;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Gestão de penalidades contratuais vinculadas a planos.
/// </summary>
[ApiController]
[Route("api/penalties")]
[Produces("application/json")]
[Tags(ApiDocumentation.Tags.Penalties)]
public sealed class PenaltyController : ControllerBase
{
    private readonly IConfirmContractPenaltyPayment _confirmPenaltyPayment;
    private readonly IWaiveContractPenalty _waivePenalty;
    private readonly ICancelContractPenalty _cancelPenalty;

    public PenaltyController(
        IConfirmContractPenaltyPayment confirmPenaltyPayment,
        IWaiveContractPenalty waivePenalty,
        ICancelContractPenalty cancelPenalty)
    {
        _confirmPenaltyPayment = confirmPenaltyPayment;
        _waivePenalty = waivePenalty;
        _cancelPenalty = cancelPenalty;
    }

    /// <summary>
    /// Confirma pagamento de uma penalidade contratual.
    /// </summary>
    /// <param name="id">Identificador da penalidade.</param>
    /// <response code="200">Pagamento confirmado com sucesso.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Penalidade não encontrada.</response>
    [Authorize]
    [HttpPatch("{id}/confirm-payment")]
    [ProducesResponseType(typeof(ApiResponse<ConfirmContractPenaltyPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPenaltyPayment(Guid id)
    {
        var result = await _confirmPenaltyPayment.Execute(id);

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
    /// Concede isenção (waive) de uma penalidade contratual.
    /// </summary>
    /// <param name="id">Identificador da penalidade.</param>
    /// <param name="input">Motivo da isenção.</param>
    /// <response code="200">Penalidade isenta com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Penalidade não encontrada.</response>
    [Authorize]
    [HttpPatch("{id}/waive")]
    [ProducesResponseType(typeof(ApiResponse<WaiveContractPenaltyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WaivePenalty(Guid id, [FromBody] WaiveContractPenaltyInput input)
    {
        var result = await _waivePenalty.Execute(id, input);

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
    /// Cancela uma penalidade contratual.
    /// </summary>
    /// <param name="id">Identificador da penalidade.</param>
    /// <param name="input">Motivo do cancelamento.</param>
    /// <response code="200">Penalidade cancelada com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Penalidade não encontrada.</response>
    [Authorize]
    [HttpPatch("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<CancelContractPenaltyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPenalty(Guid id, [FromBody] CancelContractPenaltyInput input)
    {
        var result = await _cancelPenalty.Execute(id, input);

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
