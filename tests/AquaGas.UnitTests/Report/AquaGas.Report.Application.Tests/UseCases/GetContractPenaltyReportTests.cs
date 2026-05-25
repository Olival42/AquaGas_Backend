using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.UseCases;
using AquaGas.Shared.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;
using EmployeeEntity = AquaGas.Employee.Domain.Models.Employee;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

namespace AquaGas.Report.Application.Tests.UseCases;

public sealed class GetContractPenaltyReportTests
{
    private readonly Mock<IUserContextService> _userContextService = new();
    private readonly Mock<IContractPenaltyRepository> _contractPenaltyRepository = new();
    private readonly Mock<IPlanRepository> _planRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();

    private readonly GetContractPenaltyReport _useCase;

    public GetContractPenaltyReportTests()
    {
        _useCase = new GetContractPenaltyReport(
            _userContextService.Object,
            _contractPenaltyRepository.Object,
            _planRepository.Object,
            _customerRepository.Object,
            _employeeRepository.Object);
    }

    [Fact]
    public async Task Should_Return_Forbidden_When_User_Is_Not_Manager()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Employee));

        var result = await _useCase.Execute(new ContractPenaltyReportInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Code == "FORBIDDEN");
    }

    [Fact]
    public async Task Should_Return_Report_Summary_And_Items()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var plan = CreatePlan();
        var customer = CreateCustomer(plan.CustomerId);
        var initiatorUserId = Guid.NewGuid();
        var resolverUserId = Guid.NewGuid();
        var pending = CreatePenalty(
            plan.Id,
            initiatorUserId,
            ContractPenaltyType.Downgrade,
            ContractPenaltyStatus.PendingPayment,
            100m,
            80m,
            20m);
        var paid = CreatePenalty(
            plan.Id,
            initiatorUserId,
            ContractPenaltyType.EarlyCancellation,
            ContractPenaltyStatus.Paid,
            200m,
            160m,
            40m,
            resolverUserId);
        var overdue = CreatePenalty(
            plan.Id,
            initiatorUserId,
            ContractPenaltyType.Downgrade,
            ContractPenaltyStatus.PendingPayment,
            300m,
            240m,
            60m,
            null,
            DateTime.UtcNow.Date.AddDays(-20),
            DateTime.UtcNow.Date.AddDays(-2));

        _contractPenaltyRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync([pending, paid, overdue]);

        _planRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([plan]);

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([customer]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([
                CreateEmployee(initiatorUserId, "Maria Souza"),
                CreateEmployee(resolverUserId, "Carlos Lima")
            ]);

        var result = await _useCase.Execute(new ContractPenaltyReportInput());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Summary.TotalPenalties.Should().Be(3);
        result.Value.Summary.PendingAmount.Should().Be(20m);
        result.Value.Summary.PaidAmount.Should().Be(40m);
        result.Value.Summary.WaivedAmount.Should().Be(0m);
        result.Value.Summary.OverdueAmount.Should().Be(60m);
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().Contain(x => x.Type == "DOWNGRADE");
        result.Value.Items.Should().Contain(x => x.Type == "EARLY_CANCELLATION");
        result.Value.Items.Should().Contain(x => x.Status == "OVERDUE");
        result.Value.Items.Should().Contain(x => x.Origin.Type == "PLAN_DOWNGRADE");
        result.Value.Items.Should().Contain(x => x.Origin.Type == "PLAN_EARLY_CANCELLATION");
        result.Value.Items.Should().Contain(x => x.ResolvedBy != null && x.ResolvedBy.Name == "Carlos Lima");
    }

    [Fact]
    public async Task Should_Return_Failure_When_Role_Cannot_Be_Resolved()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Fail(
                Shared.Errors.Error.Unauthorized("Unauthorized")));

        var result = await _useCase.Execute(new ContractPenaltyReportInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Code == "UNAUTHORIZED");
    }

    [Fact]
    public async Task Should_Return_Validation_Error_When_Start_Is_Greater_Than_End()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var result = await _useCase.Execute(new ContractPenaltyReportInput
        {
            Start = new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc)
        });

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Code == "VALIDATION_ERROR");
        result.Errors.Should().Contain(x => x.Field == "Period");
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Plan_Does_Not_Exist()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var penalty = CreatePenalty(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContractPenaltyType.Downgrade,
            ContractPenaltyStatus.PendingPayment,
            100m,
            80m,
            20m);

        _contractPenaltyRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync([penalty]);

        _planRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(new ContractPenaltyReportInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Message.Contains("Plan"));
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Customer_Does_Not_Exist()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var plan = CreatePlan();

        var penalty = CreatePenalty(
            plan.Id,
            Guid.NewGuid(),
            ContractPenaltyType.Downgrade,
            ContractPenaltyStatus.PendingPayment,
            100m,
            80m,
            20m);

        _contractPenaltyRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync([penalty]);

        _planRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([plan]);

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([
                CreateEmployee(penalty.InitiatedBy, "Maria Souza")
            ]);

        var result = await _useCase.Execute(new ContractPenaltyReportInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Message.Contains("Customer"));
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Initiator_Employee_Does_Not_Exist()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var plan = CreatePlan();
        var customer = CreateCustomer(plan.CustomerId);

        var penalty = CreatePenalty(
            plan.Id,
            Guid.NewGuid(),
            ContractPenaltyType.Downgrade,
            ContractPenaltyStatus.PendingPayment,
            100m,
            80m,
            20m);

        _contractPenaltyRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync([penalty]);

        _planRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([plan]);

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([customer]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(new ContractPenaltyReportInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Message.Contains("Employee for user"));
    }

    [Fact]
    public async Task Should_Return_Waived_Amount_In_Summary()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var plan = CreatePlan();
        var customer = CreateCustomer(plan.CustomerId);

        var initiatedBy = Guid.NewGuid();
        var resolvedBy = Guid.NewGuid();

        var waived = CreatePenalty(
            plan.Id,
            initiatedBy,
            ContractPenaltyType.Downgrade,
            ContractPenaltyStatus.Waived,
            200m,
            100m,
            50m,
            resolvedBy);

        _contractPenaltyRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync([waived]);

        _planRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([plan]);

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([customer]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([
                CreateEmployee(initiatedBy, "Maria Souza"),
            CreateEmployee(resolvedBy, "Carlos Lima")
            ]);

        var result = await _useCase.Execute(new ContractPenaltyReportInput());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Summary.WaivedAmount.Should().Be(50m);

        result.Value.Items[0].Status.Should().Be("WAIVED");
        result.Value.Items[0].ResolvedBy.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Return_Empty_Report_When_No_Penalties_Exist()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        _contractPenaltyRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync([]);

        _planRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(new ContractPenaltyReportInput());

        result.IsSuccess.Should().BeTrue();

        result.Value!.Summary.TotalPenalties.Should().Be(0);
        result.Value.Summary.PendingAmount.Should().Be(0);
        result.Value.Summary.PaidAmount.Should().Be(0);
        result.Value.Summary.WaivedAmount.Should().Be(0);
        result.Value.Summary.OverdueAmount.Should().Be(0);

        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Order_Items_By_Date_Descending()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var plan = CreatePlan();
        var customer = CreateCustomer(plan.CustomerId);
        var userId = Guid.NewGuid();

        var older = CreatePenalty(
            plan.Id,
            userId,
            ContractPenaltyType.Downgrade,
            ContractPenaltyStatus.PendingPayment,
            100m,
            80m,
            20m,
            null,
            new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc));

        var newer = CreatePenalty(
            plan.Id,
            userId,
            ContractPenaltyType.EarlyCancellation,
            ContractPenaltyStatus.PendingPayment,
            200m,
            150m,
            50m,
            null,
            new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc));

        _contractPenaltyRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync([older, newer]);

        _planRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([plan]);

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([customer]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([
                CreateEmployee(userId, "Maria Souza")
            ]);

        var result = await _useCase.Execute(new ContractPenaltyReportInput());

        result.IsSuccess.Should().BeTrue();

        result.Value!.Items.Should().HaveCount(2);

        result.Value.Items[0].Date.Should().Be(newer.Timestamp);
        result.Value.Items[1].Date.Should().Be(older.Timestamp);
    }

    [Fact]
    public async Task Should_Set_Audit_Flags_To_False_When_Penalty_Is_Paid()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var plan = CreatePlan();
        var customer = CreateCustomer(plan.CustomerId);

        var initiatedBy = Guid.NewGuid();
        var resolvedBy = Guid.NewGuid();

        var penalty = CreatePenalty(
            plan.Id,
            initiatedBy,
            ContractPenaltyType.Downgrade,
            ContractPenaltyStatus.Paid,
            100m,
            80m,
            20m,
            resolvedBy);

        _contractPenaltyRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync([penalty]);

        _planRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([plan]);

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([customer]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([
                CreateEmployee(initiatedBy, "Maria Souza"),
            CreateEmployee(resolvedBy, "Carlos Lima")
            ]);

        var result = await _useCase.Execute(new ContractPenaltyReportInput());

        result.IsSuccess.Should().BeTrue();

        var audit = result.Value!.Items[0].Audit;

        audit.CanBePaid.Should().BeFalse();
        audit.CanBeWaived.Should().BeFalse();
        audit.CanBeCanceled.Should().BeFalse();
    }

    private static PlanEntity CreatePlan()
    {
        return new PlanEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(500m).Value!,
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc),
            10,
            5);
    }

    private static CustomerEntity CreateCustomer(Guid id)
    {
        var customer = new CustomerEntity(
            CustomerName.Create("Joao da Silva").Value!,
            Document.Create("55964416004").Value!,
            Email.Create("joao@aquagas.com").Value!,
            Phone.Create("44999999999").Value!);

        typeof(CustomerEntity).GetProperty(nameof(CustomerEntity.Id))!
            .SetValue(customer, id);

        return customer;
    }

    private static EmployeeEntity CreateEmployee(Guid userId, string name)
    {
        var employee = new EmployeeEntity(
            EmployeeName.Create(name).Value!,
            Cpf.Create("41553651014").Value!,
            Email.Create($"{name.Replace(" ", "").ToLowerInvariant()}@aquagas.com").Value!,
            Phone.Create("44999999998").Value!);

        employee.AssignUserId(userId);
        return employee;
    }

    private static ContractPenalty CreatePenalty(
        Guid planId,
        Guid initiatedBy,
        ContractPenaltyType type,
        ContractPenaltyStatus status,
        decimal originalValue,
        decimal remainingValue,
        decimal calculatedValue,
        Guid? resolvedBy = null,
        DateTime? timestamp = null,
        DateTime? dueDate = null)
    {
        var penalty = new ContractPenalty(
            planId,
            Guid.NewGuid(),
            initiatedBy,
            type,
            Price.Create(originalValue).Value!,
            Price.Create(remainingValue).Value!,
            Price.Create(calculatedValue).Value!,
            "Observacao administrativa");

        if (timestamp.HasValue)
        {
            typeof(ContractPenalty).GetProperty(nameof(ContractPenalty.Timestamp))!
                .SetValue(penalty, timestamp.Value);
        }

        if (dueDate.HasValue)
        {
            typeof(ContractPenalty).GetProperty(nameof(ContractPenalty.DueDate))!
                .SetValue(penalty, dueDate.Value);
        }

        switch (status)
        {
            case ContractPenaltyStatus.Paid:
                penalty.Pay(resolvedBy ?? Guid.NewGuid());
                break;
            case ContractPenaltyStatus.Waived:
                penalty.Waive(resolvedBy ?? Guid.NewGuid(), "Motivo suficiente");
                break;
            case ContractPenaltyStatus.Canceled:
                penalty.Cancel(resolvedBy ?? Guid.NewGuid(), "Motivo suficiente para cancelar");
                break;
        }

        return penalty;
    }
}
