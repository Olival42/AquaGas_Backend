using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using FluentAssertions;
using Moq;
using Xunit;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class ConfirmBillingPaymentTests
{
    private readonly Mock<IBillingRepository> _billingRepo = new();
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IPlanLifecycleService> _lifecycle = new();

    private readonly ConfirmBillingPayment _useCase;

    public ConfirmBillingPaymentTests()
    {
        _useCase = new ConfirmBillingPayment(
            _billingRepo.Object,
            _planRepo.Object,
            _userContext.Object,
            _audit.Object,
            _lifecycle.Object);
    }

    private static PlanEntity CreatePlan()
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(100).Value!,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(30),
            10,
            10,
            null);

    private static Billing CreateBilling(Guid planId, int period, BillingStatus status, DateTime dueDate)
    {
        var billing = new Billing(planId, period, dueDate, Price.Create(100).Value!);
        if (status == BillingStatus.Paid)
            billing.Pay(Guid.NewGuid());
        if (status == BillingStatus.Late)
        {
            billing = new Billing(planId, period, DateTime.UtcNow.AddDays(-2), Price.Create(100).Value!);
            billing.MarkAsLate();
        }
        return billing;
    }

    [Fact]
    public async Task Should_Fail_When_Billing_Not_Found()
    {
        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _billingRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Billing?)null);

        var result = await _useCase.Execute(new ConfirmBillingPaymentInput { BillingId = Guid.NewGuid() });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_There_Is_Previous_Pending_Billing()
    {
        var plan = CreatePlan();
        var target = CreateBilling(plan.Id, 2, BillingStatus.Pending, DateTime.UtcNow.AddDays(5));
        var previous = CreateBilling(plan.Id, 1, BillingStatus.Pending, DateTime.UtcNow.AddDays(1));

        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _billingRepo.Setup(x => x.GetByIdAsync(target.Id)).ReturnsAsync(target);
        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([previous, target]);

        var result = await _useCase.Execute(new ConfirmBillingPaymentInput { BillingId = target.Id });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Is_Canceled()
    {
        var plan = CreatePlan();
        plan.Cancel();
        var billing = CreateBilling(plan.Id, 1, BillingStatus.Pending, DateTime.UtcNow.AddDays(1));

        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _billingRepo.Setup(x => x.GetByIdAsync(billing.Id)).ReturnsAsync(billing);
        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        var result = await _useCase.Execute(new ConfirmBillingPaymentInput { BillingId = billing.Id });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Billing_Already_Paid()
    {
        var plan = CreatePlan();
        var billing = CreateBilling(plan.Id, 1, BillingStatus.Paid, DateTime.UtcNow.AddDays(1));

        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _billingRepo.Setup(x => x.GetByIdAsync(billing.Id)).ReturnsAsync(billing);
        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        var result = await _useCase.Execute(new ConfirmBillingPaymentInput { BillingId = billing.Id });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Confirm_Billing_Successfully()
    {
        var plan = CreatePlan();
        var billing = CreateBilling(plan.Id, 1, BillingStatus.Pending, DateTime.UtcNow.AddDays(1));

        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _billingRepo.Setup(x => x.GetByIdAsync(billing.Id)).ReturnsAsync(billing);
        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([billing]);

        var result = await _useCase.Execute(new ConfirmBillingPaymentInput { BillingId = billing.Id });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(BillingStatus.Paid.ToString());
        _billingRepo.Verify(x => x.Update(It.IsAny<Billing>()), Times.AtLeastOnce);
        _billingRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _lifecycle.Verify(x => x.TryCompletePlanAsync(plan.Id), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_UserId_Is_Invalid()
    {
        var plan = CreatePlan();
        var billing = CreateBilling(plan.Id, 1, BillingStatus.Pending, DateTime.UtcNow.AddDays(1));

        _billingRepo.Setup(x => x.GetByIdAsync(billing.Id)).ReturnsAsync(billing);
        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("invalid user")));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(new ConfirmBillingPaymentInput
        {
            BillingId = billing.Id
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_UserName_Is_Invalid()
    {
        var plan = CreatePlan();
        var billing = CreateBilling(plan.Id, 1, BillingStatus.Pending, DateTime.UtcNow.AddDays(1));

        _billingRepo.Setup(x => x.GetByIdAsync(billing.Id)).ReturnsAsync(billing);
        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(Error.Unauthorized("invalid name")));

        var result = await _useCase.Execute(new ConfirmBillingPaymentInput
        {
            BillingId = billing.Id
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Previous_Billing_Is_Pending_Or_Late()
    {
        var plan = CreatePlan();

        var target = CreateBilling(plan.Id, 2, BillingStatus.Pending, DateTime.UtcNow.AddDays(5));
        var previous = CreateBilling(plan.Id, 1, BillingStatus.Pending, DateTime.UtcNow.AddDays(1));

        _billingRepo.Setup(x => x.GetByIdAsync(target.Id))
            .ReturnsAsync(target);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Billing> { previous, target });

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(new ConfirmBillingPaymentInput
        {
            BillingId = target.Id
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Call_Audit_Log_When_Success()
    {
        var plan = CreatePlan();
        var billing = CreateBilling(plan.Id, 1, BillingStatus.Pending, DateTime.UtcNow.AddDays(1));

        var userId = Guid.NewGuid();

        _billingRepo.Setup(x => x.GetByIdAsync(billing.Id))
            .ReturnsAsync(billing);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([billing]);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(new ConfirmBillingPaymentInput
        {
            BillingId = billing.Id
        });

        result.IsSuccess.Should().BeTrue();

        _audit.Verify(x =>
            x.LogAsync(
                userId,
                "User",
                AuditAction.UPDATE,
                "Billing",
                billing.Id,
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }
}
