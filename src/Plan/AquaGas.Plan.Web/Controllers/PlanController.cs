namespace AquaGas.Plan.Web.Controllers;

using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Shared.Http;
using AquaGas.Shared.OpenApi;
using AquaGas.Shared.Responses;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Gestão de planos de assinatura: cadastro, entregas, cobrança e ciclo de vida.
/// </summary>
[ApiController]
[Route("api/plans")]
[Produces("application/json")]
[Tags(ApiDocumentation.Tags.Plans)]
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

    /// <summary>
    /// Cadastra um novo plano de assinatura.
    /// </summary>
    /// <param name="input">Cliente, ciclo, itens e configuração de entrega/cobrança.</param>
    /// <response code="201">Plano criado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Cliente ou produto não encontrado.</response>
    /// <response code="409">Conflito de regra de negócio.</response>
    [Authorize]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<PlanResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
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

    /// <summary>
    /// Obtém plano por identificador.
    /// </summary>
    /// <param name="id">Identificador do plano.</param>
    /// <response code="200">Plano encontrado.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Plano não encontrado.</response>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getById.Execute(id);

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

    /// <summary>
    /// Lista todos os planos.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="401">Não autenticado.</response>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PlanResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Confirma a entrega de um ciclo do plano.
    /// </summary>
    /// <param name="input">Identificador da entrega e dados de confirmação.</param>
    /// <response code="200">Entrega confirmada com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Entrega não encontrada.</response>
    [Authorize]
    [HttpPatch("confirm-delivery")]
    [ProducesResponseType(typeof(ApiResponse<ConfirmDeliveryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Cancela uma entrega agendada.
    /// </summary>
    /// <param name="input">Identificador da entrega e motivo.</param>
    /// <response code="200">Entrega cancelada com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Entrega não encontrada.</response>
    [Authorize]
    [HttpPatch("cancel-delivery")]
    [ProducesResponseType(typeof(ApiResponse<CancelDeliveryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Reagenda uma entrega.
    /// </summary>
    /// <param name="input">Nova data e identificador da entrega.</param>
    /// <response code="200">Entrega reagendada com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Entrega não encontrada.</response>
    [Authorize]
    [HttpPatch("reschedule-delivery")]
    [ProducesResponseType(typeof(ApiResponse<RescheduleDeliveryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Suspende um plano ativo.
    /// </summary>
    /// <param name="id">Identificador do plano.</param>
    /// <param name="input">Motivo da suspensão.</param>
    /// <response code="200">Plano suspenso com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Plano não encontrado.</response>
    [Authorize]
    [HttpPatch("{id:guid}/suspend")]
    [ProducesResponseType(typeof(ApiResponse<SuspendPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Reativa um plano suspenso.
    /// </summary>
    /// <param name="id">Identificador do plano.</param>
    /// <response code="200">Plano reativado com sucesso.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Plano não encontrado.</response>
    [Authorize]
    [HttpPatch("{id:guid}/reactivate")]
    [ProducesResponseType(typeof(ApiResponse<ReactivatePlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var result = await _reactivatePlan.Execute(id);

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

    /// <summary>
    /// Cancela um plano de assinatura.
    /// </summary>
    /// <param name="id">Identificador do plano.</param>
    /// <param name="input">Motivo do cancelamento.</param>
    /// <response code="200">Plano cancelado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Plano não encontrado.</response>
    [Authorize]
    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<CancelPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Realiza upgrade do plano (amplia itens ou quantidades).
    /// </summary>
    /// <param name="id">Identificador do plano.</param>
    /// <param name="input">Novos itens ou configurações.</param>
    /// <response code="200">Upgrade aplicado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Plano não encontrado.</response>
    [Authorize]
    [HttpPatch("{id:guid}/upgrade")]
    [ProducesResponseType(typeof(ApiResponse<UpgradePlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Realiza downgrade do plano (reduz itens ou quantidades).
    /// </summary>
    /// <param name="id">Identificador do plano.</param>
    /// <param name="input">Itens reduzidos ou nova configuração.</param>
    /// <response code="200">Downgrade aplicado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Plano não encontrado.</response>
    [Authorize]
    [HttpPatch("{id:guid}/downgrade")]
    [ProducesResponseType(typeof(ApiResponse<DowngradePlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Confirma pagamento da cobrança do plano.
    /// </summary>
    /// <param name="input">Identificador da cobrança e dados do pagamento.</param>
    /// <response code="200">Pagamento confirmado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Cobrança não encontrada.</response>
    [Authorize]
    [HttpPatch("confirm-billing-payment")]
    [ProducesResponseType(typeof(ApiResponse<ConfirmBillingPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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
