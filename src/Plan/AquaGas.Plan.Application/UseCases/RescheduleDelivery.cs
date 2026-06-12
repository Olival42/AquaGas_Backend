using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public sealed class RescheduleDelivery : IRescheduleDelivery
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUserContextService _userContext;
    private readonly IAuditLogService _auditLogService;

    public RescheduleDelivery(
        IDeliveryRepository deliveryRepository,
        IPlanRepository planRepository,
        IUserContextService userContext,
        IAuditLogService auditLogService)
    {
        _deliveryRepository = deliveryRepository;
        _planRepository = planRepository;
        _userContext = userContext;
        _auditLogService = auditLogService;
    }

    public async Task<Result<RescheduleDeliveryResponse>> Execute(RescheduleDeliveryInput input)
    {
        var delivery = await _deliveryRepository.GetByIdAsync(input.DeliveryId);
        if (delivery is null)
            return Result<RescheduleDeliveryResponse>.Fail(Error.NotFound("Delivery not found"));

        var plan = await _planRepository.GetByIdAsync(delivery.PlanId);
        if (plan is null)
            return Result<RescheduleDeliveryResponse>.Fail(Error.NotFound("Plan not found"));

        if (plan.Status == PlanStatus.Canceled || plan.Status == PlanStatus.Suspended)
            return Result<RescheduleDeliveryResponse>.Fail(
                Error.Conflict($"Cannot reschedule delivery for a {plan.Status.ToString().ToLower()} plan"));

        if (delivery.Status != DeliveryStatus.Canceled)
            return Result<RescheduleDeliveryResponse>.Fail(Error.Conflict("Only canceled deliveries can be rescheduled"));

        // A nova data pode ser antecipada sem limite (desde que no futuro — validado no input).
        // O teto de 7 dias permanece apenas para adiamentos (diferença positiva).
        var diff = (input.NewDate.Date - delivery.DueDate.Date).Days;
        if (diff > 7)
            return Result<RescheduleDeliveryResponse>.Fail(
                Error.Conflict(
                    $"The new date cannot be postponed more than 7 days beyond the original expected date ({delivery.DueDate:dd/MM/yyyy})."));

        var userIdResult = _userContext.GetUserId();
        var userNameResult = _userContext.GetUserName();

        if (userIdResult.IsFailure)
            return Result<RescheduleDeliveryResponse>.Fail(userIdResult.Errors.ToArray());

        if (userNameResult.IsFailure)
            return Result<RescheduleDeliveryResponse>.Fail(userNameResult.Errors.ToArray());

        var previousDate = delivery.DueDate;
        delivery.RescheduleManually(input.NewDate);
        _deliveryRepository.Update(delivery);

        await _auditLogService.LogAsync(
            userIdResult.Value!,
            userNameResult.Value ?? "System",
            AuditAction.UPDATE,
            "Delivery",
            delivery.Id,
            new { PreviousDate = previousDate, PreviousStatus = DeliveryStatus.Canceled.ToString() },
            new
            {
                delivery.Id,
                NewDate = delivery.DueDate,
                Status = delivery.Status.ToString(),
                input.Reason,
                Action = "RESCHEDULE"
            });

        await _deliveryRepository.SaveChangesAsync();

        return Result<RescheduleDeliveryResponse>.Success(
            new RescheduleDeliveryResponse
            {
                DeliveryId = delivery.Id,
                PreviousDate = previousDate,
                NewDate = delivery.DueDate,
                Status = delivery.Status.ToString(),
                Message = "Delivery rescheduled successfully"
            });
    }
}
