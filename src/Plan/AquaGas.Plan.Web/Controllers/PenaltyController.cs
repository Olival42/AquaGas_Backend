namespace AquaGas.Plan.Web.Controllers;

using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Shared.Http;
using AquaGas.Shared.Responses;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/penalties")]
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

    [Authorize]
    [HttpPatch("{id}/confirm-payment")]
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

    [Authorize]
    [HttpPatch("{id}/waive")]
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

    [Authorize]
    [HttpPatch("{id}/cancel")]
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
