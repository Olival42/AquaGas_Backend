using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.Dtos.Responses;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Report.Application.UseCases;

public sealed class GetContractPenaltyReport
    : IGetContractPenaltyReport
{
    private readonly IUserContextService _userContextService;
    private readonly IContractPenaltyRepository _contractPenaltyRepository;
    private readonly IPlanRepository _planRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public GetContractPenaltyReport(
        IUserContextService userContextService,
        IContractPenaltyRepository contractPenaltyRepository,
        IPlanRepository planRepository,
        ICustomerRepository customerRepository,
        IEmployeeRepository employeeRepository)
    {
        _userContextService = userContextService;
        _contractPenaltyRepository = contractPenaltyRepository;
        _planRepository = planRepository;
        _customerRepository = customerRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<ContractPenaltyReportResponse>> Execute(
        ContractPenaltyReportInput input)
    {
        var roleResult = _userContextService.GetRole();

        if (roleResult.IsFailure)
            return Result<ContractPenaltyReportResponse>.Fail(roleResult.Errors.ToArray());

        if (roleResult.Value != Role.Manager)
            return Result<ContractPenaltyReportResponse>.Fail(
                Error.Forbidden("Only managers can access contract penalty reports"));

        var startUtc = input.Start.HasValue ? NormalizeUtc(input.Start.Value) : (DateTime?)null;
        var endUtc = input.End.HasValue ? NormalizeUtc(input.End.Value) : (DateTime?)null;

        if (startUtc.HasValue && endUtc.HasValue && startUtc > endUtc)
            return Result<ContractPenaltyReportResponse>.Fail(
                Error.Validation(
                    "Start must be less than or equal to End",
                    "Period"));

        var penalties = await _contractPenaltyRepository.GetReportAsync(
            startUtc,
            endUtc);

        var effectivePenalties = penalties
            .Select(x => new
            {
                Penalty = x,
                EffectiveStatus = GetEffectiveStatus(x)
            })
            .OrderByDescending(x => x.Penalty.Timestamp)
            .ToList();

        var plans = await _planRepository.GetByIdsAsync(penalties.Select(x => x.PlanId));
        var plansById = plans.ToDictionary(x => x.Id);

        var customers = await _customerRepository.GetByIdsAsync(
            plans.Select(x => x.CustomerId).Distinct(),
            false);
        var customersById = customers.ToDictionary(x => x.Id);

        var userIds = penalties
            .Select(x => x.InitiatedBy)
            .Concat(penalties.Where(x => x.PaidBy.HasValue).Select(x => x.PaidBy!.Value))
            .Concat(penalties.Where(x => x.WaivedBy.HasValue).Select(x => x.WaivedBy!.Value))
            .Concat(penalties.Where(x => x.CanceledBy.HasValue).Select(x => x.CanceledBy!.Value))
            .Distinct()
            .ToList();

        var employees = await _employeeRepository.GetByUserIdsAsync(userIds, false);
        var employeesByUserId = employees.ToDictionary(x => x.UserId);

        var items = new List<ContractPenaltyReportItemResponse>(effectivePenalties.Count);

        foreach (var row in effectivePenalties)
        {
            var penalty = row.Penalty;

            if (!plansById.TryGetValue(penalty.PlanId, out var plan))
                return Result<ContractPenaltyReportResponse>.Fail(
                    Error.NotFound($"Plan {penalty.PlanId} not found"));

            if (!customersById.TryGetValue(plan.CustomerId, out var customer))
                return Result<ContractPenaltyReportResponse>.Fail(
                    Error.NotFound($"Customer {plan.CustomerId} not found"));

            if (!employeesByUserId.TryGetValue(penalty.InitiatedBy, out var createdBy))
                return Result<ContractPenaltyReportResponse>.Fail(
                    Error.NotFound($"Employee for user {penalty.InitiatedBy} not found"));

            var resolvedBy = ResolveEmployee(
                penalty,
                row.EffectiveStatus,
                employeesByUserId);

            items.Add(new ContractPenaltyReportItemResponse
            {
                Id = $"penalty-{penalty.Id}",
                Date = NormalizeUtc(penalty.Timestamp),
                Plan = new ContractPenaltyPlanResponse
                {
                    Id = plan.Id,
                    Status = ToScreamingSnakeCase(plan.Status.ToString()),
                    Cycle = ToScreamingSnakeCase(plan.Cycle.ToString())
                },
                Customer = new CustomerSalesResponse
                {
                    Id = customer.Id,
                    Name = customer.Name.Value
                },
                Origin = new ContractPenaltyOriginResponse
                {
                    Type = penalty.Type == ContractPenaltyType.Downgrade
                        ? "PLAN_DOWNGRADE"
                        : "PLAN_EARLY_CANCELLATION",
                    Id = plan.Id
                },
                Type = ToScreamingSnakeCase(penalty.Type.ToString()),
                Financial = new ContractPenaltyFinancialResponse
                {
                    OriginalValue = penalty.OriginalValue.Value,
                    RemainingValue = penalty.RemainingValue.Value,
                    CalculatedValue = penalty.CalculatedAmount.Value
                },
                Status = ToScreamingSnakeCase(row.EffectiveStatus.ToString()),
                DueDate = NormalizeUtc(penalty.DueDate),
                PaidAt = penalty.PaidDate.HasValue ? NormalizeUtc(penalty.PaidDate.Value) : null,
                Notes = penalty.Notes,
                CreatedBy = new EmployeeSalesResponse
                {
                    Id = createdBy.Id,
                    Name = createdBy.Name.Value
                },
                ResolvedBy = resolvedBy,
                Audit = new ContractPenaltyAuditResponse
                {
                    CanBePaid = row.EffectiveStatus == ContractPenaltyStatus.PendingPayment ||
                                row.EffectiveStatus == ContractPenaltyStatus.Overdue,
                    CanBeWaived = row.EffectiveStatus == ContractPenaltyStatus.PendingPayment ||
                                  row.EffectiveStatus == ContractPenaltyStatus.Overdue,
                    CanBeCanceled = row.EffectiveStatus == ContractPenaltyStatus.PendingPayment ||
                                    row.EffectiveStatus == ContractPenaltyStatus.Overdue
                }
            });
        }

        return Result<ContractPenaltyReportResponse>.Success(
            new ContractPenaltyReportResponse
            {
                Summary = new ContractPenaltySummaryResponse
                {
                    TotalPenalties = effectivePenalties.Count,
                    PendingAmount = effectivePenalties
                        .Where(x => x.EffectiveStatus == ContractPenaltyStatus.PendingPayment)
                        .Sum(x => x.Penalty.CalculatedAmount.Value),
                    PaidAmount = effectivePenalties
                        .Where(x => x.EffectiveStatus == ContractPenaltyStatus.Paid)
                        .Sum(x => x.Penalty.CalculatedAmount.Value),
                    WaivedAmount = effectivePenalties
                        .Where(x => x.EffectiveStatus == ContractPenaltyStatus.Waived)
                        .Sum(x => x.Penalty.CalculatedAmount.Value),
                    OverdueAmount = effectivePenalties
                        .Where(x => x.EffectiveStatus == ContractPenaltyStatus.Overdue)
                        .Sum(x => x.Penalty.CalculatedAmount.Value)
                },
                Items = items
            });
    }

    private static ContractPenaltyStatus GetEffectiveStatus(
        ContractPenalty penalty)
    {
        if (penalty.Status == ContractPenaltyStatus.PendingPayment &&
            DateTime.UtcNow.Date > penalty.DueDate.Date)
            return ContractPenaltyStatus.Overdue;

        return penalty.Status;
    }

    private static EmployeeSalesResponse? ResolveEmployee(
        ContractPenalty penalty,
        ContractPenaltyStatus effectiveStatus,
        IReadOnlyDictionary<Guid, AquaGas.Employee.Domain.Models.Employee> employeesByUserId)
    {
        Guid? userId = effectiveStatus switch
        {
            ContractPenaltyStatus.Paid => penalty.PaidBy,
            ContractPenaltyStatus.Waived => penalty.WaivedBy,
            ContractPenaltyStatus.Canceled => penalty.CanceledBy,
            _ => null
        };

        if (!userId.HasValue)
            return null;

        if (!employeesByUserId.TryGetValue(userId.Value, out var employee))
            return null;

        return new EmployeeSalesResponse
        {
            Id = employee.Id,
            Name = employee.Name.Value
        };
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static string ToScreamingSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return string.Concat(
            value.Select((ch, index) =>
                index > 0 && char.IsUpper(ch)
                    ? $"_{char.ToUpperInvariant(ch)}"
                    : char.ToUpperInvariant(ch).ToString()));
    }
}
