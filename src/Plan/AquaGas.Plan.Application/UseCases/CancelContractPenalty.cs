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

public sealed class CancelContractPenalty : ICancelContractPenalty
{
    private readonly IContractPenaltyRepository _penaltyRepository;
    private readonly IUserContextService _userContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IPlanLifecycleService _planLifecycleService;

    public CancelContractPenalty(
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

    public async Task<Result<CancelContractPenaltyResponse>> Execute(Guid id, CancelContractPenaltyInput input)
    {
        var penalty = await _penaltyRepository.GetByIdAsync(id);
        if (penalty is null)
            return Result<CancelContractPenaltyResponse>.Fail(Error.NotFound("Contract penalty not found."));

        if (penalty.Status == ContractPenaltyStatus.Paid)
            return Result<CancelContractPenaltyResponse>.Fail(Error.Conflict("Penalty is already paid and cannot be canceled."));

        if (penalty.Status == ContractPenaltyStatus.Waived)
            return Result<CancelContractPenaltyResponse>.Fail(Error.Conflict("Penalty is already waived and cannot be canceled."));

        if (penalty.Status == ContractPenaltyStatus.Canceled)
            return Result<CancelContractPenaltyResponse>.Fail(Error.Conflict("Penalty is already canceled"));

        if (penalty.Status != ContractPenaltyStatus.PendingPayment && penalty.Status != ContractPenaltyStatus.Overdue)
            return Result<CancelContractPenaltyResponse>.Fail(Error.Conflict($"Penalty status '{penalty.Status}' does not allow cancellation."));

        if (penalty.Timestamp < DateTime.UtcNow.AddYears(-1))
            return Result<CancelContractPenaltyResponse>.Fail(Error.Validation("This penalty is too old to be canceled."));

        var userIdResult = _userContext.GetUserId();
        var userNameResult = _userContext.GetUserName();

        if (userIdResult.IsFailure)
            return Result<CancelContractPenaltyResponse>.Fail(userIdResult.Errors.ToArray());

        if (userNameResult.IsFailure)
            return Result<CancelContractPenaltyResponse>.Fail(userNameResult.Errors.ToArray());

        var previousStatus = penalty.Status;
        var userId = userIdResult.Value!;

        penalty.Cancel(userId, input.Reason);
        _penaltyRepository.Update(penalty);

        await _auditLogService.LogAsync(
            userId,
            userNameResult.Value ?? "System",
            AuditAction.UPDATE,
            "ContractPenalty",
            penalty.Id,
            new
            {
                Status = previousStatus.ToString(),
                Amount = penalty.CalculatedAmount.Value
            },
            new
            {
                penalty.Id,
                Status = penalty.Status.ToString(),
                input.Reason,
                CanceledBy = userId,
                penalty.CanceledAt,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = penalty.Status.ToString(),
                PenaltyAmount = penalty.CalculatedAmount.Value,
                PenaltyType = penalty.Type.ToString(),
            });

        await _penaltyRepository.SaveChangesAsync();
        await _planLifecycleService.TryCompletePlanAsync(penalty.PlanId);

        return Result<CancelContractPenaltyResponse>.Success(new CancelContractPenaltyResponse
        {
            PenaltyId = penalty.Id,
            Status = penalty.Status.ToString(),
            Amount = penalty.CalculatedAmount.Value,
            CanceledAt = penalty.CanceledAt,
            Reason = input.Reason,
            Message = "Contract penalty canceled successfully"
        });
    }
}
