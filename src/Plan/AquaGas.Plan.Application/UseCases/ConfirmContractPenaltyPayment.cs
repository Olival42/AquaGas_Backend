using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public sealed class ConfirmContractPenaltyPayment : IConfirmContractPenaltyPayment
{
    private readonly IContractPenaltyRepository _penaltyRepository;
    private readonly IUserContextService _userContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IPlanLifecycleService _planLifecycleService;

    public ConfirmContractPenaltyPayment(
        IContractPenaltyRepository penaltyRepository,
        IUserContextService userContext,
        IAuditLogService auditLogService,
        IPlanLifecycleService planLifecycleService)
    {
        _penaltyRepository = penaltyRepository;
        _userContext = userContext;
        _auditLogService = auditLogService;
        _planLifecycleService = planLifecycleService;
    }

    public async Task<Result<ConfirmContractPenaltyPaymentResponse>> Execute(Guid id)
    {
        var penalty = await _penaltyRepository.GetByIdAsync(id);
        if (penalty is null)
            return Result<ConfirmContractPenaltyPaymentResponse>.Fail(Error.NotFound("Contract penalty not found"));

        if (penalty.Status == ContractPenaltyStatus.Paid)
            return Result<ConfirmContractPenaltyPaymentResponse>.Fail(Error.Conflict("Penalty is already paid"));

        if (penalty.Status == ContractPenaltyStatus.Waived)
            return Result<ConfirmContractPenaltyPaymentResponse>.Fail(Error.Conflict("Penalty is waived and cannot be paid"));

        if (penalty.Status == ContractPenaltyStatus.Canceled)
            return Result<ConfirmContractPenaltyPaymentResponse>.Fail(
                Error.Conflict("Cancelled penalties cannot be paid"));

        if (penalty.Status != ContractPenaltyStatus.PendingPayment && penalty.Status != ContractPenaltyStatus.Overdue)
            return Result<ConfirmContractPenaltyPaymentResponse>.Fail(Error.Conflict($"Penalty status '{penalty.Status}' does not allow payment"));

        if (penalty.Timestamp < DateTime.UtcNow.AddYears(-1))
            return Result<ConfirmContractPenaltyPaymentResponse>.Fail(
                Error.Validation("This penalty can no longer be paid because it exceeds the allowed payment period"));

        if (penalty.CalculatedAmount.Value <= 0)
            return Result<ConfirmContractPenaltyPaymentResponse>.Fail(
                Error.Validation("Cannot confirm payment for a zero amount penalty"));

        var userIdResult = _userContext.GetUserId();
        var userNameResult = _userContext.GetUserName();

        if (userIdResult.IsFailure)
            return Result<ConfirmContractPenaltyPaymentResponse>.Fail(userIdResult.Errors.ToArray());

        if (userNameResult.IsFailure)
            return Result<ConfirmContractPenaltyPaymentResponse>.Fail(userNameResult.Errors.ToArray());

        var previousStatus = penalty.Status;
        var userId = userIdResult.Value!;

        penalty.Pay(userId);
        _penaltyRepository.Update(penalty);

        await _auditLogService.LogAsync(
            userId,
            userNameResult.Value ?? "System",
            AuditAction.UPDATE,
            "ContractPenalty",
            penalty.Id,
            new
            {
                PreviousStatus = previousStatus.ToString(),
                PreviousPaidAt = penalty.PaidDate
            },
            new
            {
                penalty.Id,
                NewStatus = penalty.Status.ToString(),
                Amount = penalty.CalculatedAmount.Value,
                PaidAt = penalty.PaidDate,
                PaidBy = userId
            });

        await _penaltyRepository.SaveChangesAsync();
        await _planLifecycleService.TryCompletePlanAsync(penalty.PlanId);

        return Result<ConfirmContractPenaltyPaymentResponse>.Success(new ConfirmContractPenaltyPaymentResponse
        {
            PenaltyId = penalty.Id,
            Status = penalty.Status.ToString(),
            PaidAt = penalty.PaidDate,
            Amount = penalty.CalculatedAmount.Value,
            Message = "Contract penalty payment confirmed successfully."
        });
    }
}
