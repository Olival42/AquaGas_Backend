using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public sealed class ConfirmBillingPayment : IConfirmBillingPayment
{
    private readonly IBillingRepository _billingRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUserContextService _userContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IPlanLifecycleService _planLifecycleService;

    public ConfirmBillingPayment(
        IBillingRepository billingRepository,
        IPlanRepository planRepository,
        IUserContextService userContext,
        IAuditLogService auditLogService,
        IPlanLifecycleService planLifecycleService)
    {
        _billingRepository = billingRepository;
        _planRepository = planRepository;
        _userContext = userContext;
        _auditLogService = auditLogService;
        _planLifecycleService = planLifecycleService;
    }

    public async Task<Result<ConfirmBillingPaymentResponse>> Execute(ConfirmBillingPaymentInput input)
    {
        var userIdResult = _userContext.GetUserId();
        if (userIdResult.IsFailure)
            return Result<ConfirmBillingPaymentResponse>.Fail(userIdResult.Errors.ToArray());

        var userNameResult = _userContext.GetUserName();
        if (userNameResult.IsFailure)
            return Result<ConfirmBillingPaymentResponse>.Fail(userNameResult.Errors.ToArray());

        var billing = await _billingRepository.GetByIdAsync(input.BillingId);
        if (billing is null)
            return Result<ConfirmBillingPaymentResponse>.Fail(Error.NotFound("Billing not found"));

        var plan = await _planRepository.GetByIdAsync(billing.PlanId);

        if (plan is null)
            return Result<ConfirmBillingPaymentResponse>
                .Fail(Error.NotFound("Plan not found"));

        if (plan.Status == PlanStatus.Canceled)
            return Result<ConfirmBillingPaymentResponse>
                .Fail(Error.Conflict(
                    "Cannot confirm payment for a canceled plan"));

        if (billing.Status == BillingStatus.Paid)
            return Result<ConfirmBillingPaymentResponse>
                .Fail(Error.Conflict("Billing already paid"));

        var allBillings = await _billingRepository.GetByPlanIdAsync(billing.PlanId);

        foreach (var item in allBillings
             .Where(x => x.Status == BillingStatus.Pending))
        {
            item.MarkAsLate();
            _billingRepository.Update(item);
        }

        var previousPending = allBillings
            .Where(x => x.DueDate < billing.DueDate &&
                       (x.Status == BillingStatus.Pending ||
                        x.Status == BillingStatus.Late))
            .OrderBy(x => x.DueDate)
            .FirstOrDefault();

        if (previousPending is not null)
            return Result<ConfirmBillingPaymentResponse>.Fail(Error.Conflict(
                $"Cannot pay this billing because the billing due on {previousPending.DueDate:dd/MM/yyyy} is still pending or late."));

        var previousStatus = billing.Status;
        var previousPaidAt = billing.PaidAt;
        billing.Pay(userIdResult.Value!);
        _billingRepository.Update(billing);

        await _auditLogService.LogAsync(
            userIdResult.Value!,
            userNameResult.Value,
            AuditAction.UPDATE,
            "Billing",
            billing.Id,
            new
            {
                Status = previousStatus.ToString(),
                PaidAt = previousPaidAt
            },
            new
            {
                billing.Id,
                billing.PlanId,
                Amount = billing.Amount.Value,
                Status = billing.Status.ToString(),
                billing.PaidAt,
                billing.ReceivedBy
            });

        await _billingRepository.SaveChangesAsync();
        await _planLifecycleService.TryCompletePlanAsync(billing.PlanId);

        return Result<ConfirmBillingPaymentResponse>.Success(new ConfirmBillingPaymentResponse
        {
            BillingId = billing.Id,
            Status = billing.Status.ToString(),
            PaidAt = billing.PaidAt,
            ReceivedBy = billing.ReceivedBy,
            Message = "Payment confirmed successfully"
        });
    }
}
