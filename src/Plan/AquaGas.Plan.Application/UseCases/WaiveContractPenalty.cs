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

public sealed class WaiveContractPenalty : IWaiveContractPenalty
{
    private readonly IContractPenaltyRepository _penaltyRepository;
    private readonly IUserContextService _userContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IPlanLifecycleService _planLifecycleService;

    public WaiveContractPenalty(
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

    public async Task<Result<WaiveContractPenaltyResponse>> Execute(Guid id, WaiveContractPenaltyInput input)
    {
        var penalty = await _penaltyRepository.GetByIdAsync(id);
        if (penalty is null)
            return Result<WaiveContractPenaltyResponse>
                .Fail(Error.NotFound("Contract penalty not found"));

        if (penalty.Status == ContractPenaltyStatus.Paid)
            return Result<WaiveContractPenaltyResponse>
                .Fail(Error.Conflict("Penalty is already paid and cannot be waived"));

        if (penalty.Status == ContractPenaltyStatus.Waived)
            return Result<WaiveContractPenaltyResponse>
                .Fail(Error.Conflict("Penalty is already waived"));

        if (penalty.Status == ContractPenaltyStatus.Canceled)
            return Result<WaiveContractPenaltyResponse>.Fail(
                Error.Conflict("Cancelled penalties cannot be waived"));

        if (penalty.Timestamp < DateTime.UtcNow.AddYears(-1))
            return Result<WaiveContractPenaltyResponse>.Fail(
                Error.Validation("The waiver period for this penalty has expired"));

        var userIdResult = _userContext.GetUserId();
        var userNameResult = _userContext.GetUserName();

        if (userIdResult.IsFailure)
            return Result<WaiveContractPenaltyResponse>.Fail(userIdResult.Errors.ToArray());

        if (userNameResult.IsFailure)
            return Result<WaiveContractPenaltyResponse>.Fail(userNameResult.Errors.ToArray());

        var previousStatus = penalty.Status;
        var userId = userIdResult.Value!;

        penalty.Waive(userId, input.Reason);
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
                PreviousReason = penalty.WaiveReason
            },
            new
            {
                penalty.Id,
                Status = penalty.Status.ToString(),
                input.Reason,
                WaivedBy = userId,
                penalty.WaivedAt
            });

        await _penaltyRepository.SaveChangesAsync();
        await _planLifecycleService.TryCompletePlanAsync(penalty.PlanId);

        return Result<WaiveContractPenaltyResponse>.Success(new WaiveContractPenaltyResponse
        {
            PenaltyId = penalty.Id,
            Status = penalty.Status.ToString(),
            Reason = input.Reason,
            Amount = penalty.CalculatedAmount.Value,
            WaivedAt = penalty.WaivedAt,
            Message = $"Contract penalty waived successfully by {userNameResult.Value}"
        });
    }
}
