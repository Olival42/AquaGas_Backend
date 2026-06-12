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

public sealed class CancelDelivery : ICancelDelivery
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IUserContextService _userContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IPlanLifecycleService _planLifecycleService;

    public CancelDelivery(
        IDeliveryRepository deliveryRepository,
        IUserContextService userContext,
        IAuditLogService auditLogService,
        IPlanLifecycleService planLifecycleService)
    {
        _deliveryRepository = deliveryRepository;
        _userContext = userContext;
        _auditLogService = auditLogService;
        _planLifecycleService = planLifecycleService;
    }

    public async Task<Result<CancelDeliveryResponse>> Execute(CancelDeliveryInput input)
    {
        var delivery = await _deliveryRepository.GetByIdAsync(input.DeliveryId);
        if (delivery is null)
            return Result<CancelDeliveryResponse>.Fail(Error.NotFound("Delivery not found"));

        if (delivery.Status == DeliveryStatus.Delivered)
            return Result<CancelDeliveryResponse>
                .Fail(Error.Conflict("Cannot cancel a delivery that has already been delivered"));

        if (delivery.Status == DeliveryStatus.Canceled)
            return Result<CancelDeliveryResponse>.Fail(Error.Conflict("Delivery is already canceled"));

        if (delivery.DueDate.Date < DateTime.UtcNow.Date)
            return Result<CancelDeliveryResponse>.Fail(
                Error.Conflict("Cannot cancel past deliveries"));

        if (delivery.DueDate.Date == DateTime.UtcNow.Date)
            return Result<CancelDeliveryResponse>.Fail(
                Error.Conflict(
                    "Cannot cancel delivery scheduled for today"));

        var userIdResult = _userContext.GetUserId();
        var userNameResult = _userContext.GetUserName();

        if (userIdResult.IsFailure)
            return Result<CancelDeliveryResponse>.Fail(userIdResult.Errors.ToArray());

        if (userNameResult.IsFailure)
            return Result<CancelDeliveryResponse>.Fail(userIdResult.Errors.ToArray());

        var previousStatus = delivery.Status;
        delivery.Cancel();
        _deliveryRepository.Update(delivery);

        await _auditLogService.LogAsync(
            userIdResult.Value!,
            userNameResult.Value ?? "System",
            AuditAction.UPDATE,
            "Delivery",
            delivery.Id,
            new { Status = previousStatus.ToString() },
            new
            {
                delivery.Id,
                delivery.PlanId,
                Status = delivery.Status.ToString(),
                input.Reason,
                CanceledAt = DateTime.UtcNow
            });

        await _deliveryRepository.SaveChangesAsync();
        await _planLifecycleService.TryCompletePlanAsync(delivery.PlanId);

        return Result<CancelDeliveryResponse>.Success(
            new CancelDeliveryResponse
            {
                DeliveryId = delivery.Id,
                Status = delivery.Status.ToString(),
                Reason = input.Reason,
                Message = "Delivery canceled successfully"
            });
    }
}
