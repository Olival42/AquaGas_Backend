namespace AquaGas.Plan.Web.Controllers;

using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Shared.Http;
using AquaGas.Shared.Responses;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/plans")]
public sealed class PlanController : ControllerBase
{
    private readonly IRegisterPlan _registerPlan;
    private readonly IGetById _getById;
    private readonly IGetAllPlans _getAll;
    private readonly IConfirmDelivery _confirmDelivery;
    private readonly ICancelDelivery _cancelDelivery;
    private readonly IRescheduleDelivery _rescheduleDelivery;
    private readonly IConfirmBillingPayment _confirmBillingPayment;
    private readonly ISuspendPlan _suspendPlan;
    private readonly IReactivatePlan _reactivatePlan;
    private readonly ICancelPlan _cancelPlan;
    private readonly IUpgradePlan _upgradePlan;
    private readonly IDowngradePlan _downgradePlan;

    public PlanController(
        IRegisterPlan registerPlan,
        IGetById getById,
        IGetAllPlans getAll,
        IConfirmDelivery confirmDelivery,
        ICancelDelivery cancelDelivery,
        IRescheduleDelivery rescheduleDelivery,
        IConfirmBillingPayment confirmBillingPayment,
        ISuspendPlan suspendPlan,
        IReactivatePlan reactivatePlan,
        ICancelPlan cancelPlan,
        IUpgradePlan upgradePlan,
        IDowngradePlan downgradePlan)
    {
        _registerPlan = registerPlan;
        _getById = getById;
        _getAll = getAll;
        _confirmDelivery = confirmDelivery;
        _cancelDelivery = cancelDelivery;
        _rescheduleDelivery = rescheduleDelivery;
        _confirmBillingPayment = confirmBillingPayment;
        _suspendPlan = suspendPlan;
        _reactivatePlan = reactivatePlan;
        _cancelPlan = cancelPlan;
        _upgradePlan = upgradePlan;
        _downgradePlan = downgradePlan;
    }

    [Authorize]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterPlanInput input)
    {
        var result = await _registerPlan.Execute(input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse());
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.ToApiResponse()
        );
    }

    [Authorize]
    [HttpGet("{Id}")]
    public async Task<IActionResult> GetById(Guid Id)
    {
        var result = await _getById.Execute(Id);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _getAll.Execute();

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize]
    [HttpPatch("confirm-delivery")]
    public async Task<IActionResult> ConfirmDelivery([FromBody] ConfirmDeliveryInput input)
    {
        var result = await _confirmDelivery.Execute(input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize]
    [HttpPatch("cancel-delivery")]
    public async Task<IActionResult> CancelDelivery([FromBody] CancelDeliveryInput input)
    {
        var result = await _cancelDelivery.Execute(input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize]
    [HttpPatch("reschedule-delivery")]
    public async Task<IActionResult> RescheduleDelivery([FromBody] RescheduleDeliveryInput input)
    {
        var result = await _rescheduleDelivery.Execute(input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize]
    [HttpPatch("{id}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendPlanInput input)
    {
        var result = await _suspendPlan.Execute(id, input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize]
    [HttpPatch("{Id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid Id)
    {
        var result = await _reactivatePlan.Execute(Id);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize]
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelPlanInput input)
    {
        var result = await _cancelPlan.Execute(id, input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize]
    [HttpPatch("{id}/upgrade")]
    public async Task<IActionResult> Upgrade(Guid id, [FromBody] UpgradePlanInput input)
    {
        var result = await _upgradePlan.Execute(id, input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return Ok(result.ToApiResponse());
    }

    [Authorize]
    [HttpPatch("{id}/downgrade")]
    public async Task<IActionResult> Downgrade(Guid id, [FromBody] DowngradePlanInput input)
    {
        var result = await _downgradePlan.Execute(id, input);

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
    [HttpPatch("confirm-billing-payment")]
    public async Task<IActionResult> ConfirmBillingPayment([FromBody] ConfirmBillingPaymentInput input)
    {
        var result = await _confirmBillingPayment.Execute(input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return Ok(result.ToApiResponse());
    }
}
