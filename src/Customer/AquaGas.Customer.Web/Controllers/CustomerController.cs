namespace AquaGas.Api.Modules.Customer.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AquaGas.Customer.Application.UseCases;
using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Application.Dtos.Responses;
using AquaGas.Shared.Http;
using AquaGas.Shared.Responses;
using AquaGas.Shared.OpenApi;

/// <summary>
/// Operações de cadastro, consulta, atualização e histórico de consumo de clientes.
/// </summary>
[ApiController]
[Route("api/customers")]
[Produces("application/json")]
[Tags(ApiDocumentation.Tags.Customers)]
public class CustomerController : ControllerBase
{
    private readonly IRegisterCustomer _registerCustomer;
    private readonly IExistsCustomerByDocument _existsCustomerByDocument;
    private readonly IGetByCustomerId _getByCustomerId;
    private readonly IGetAllCustomers _getAllCustomers;
    private readonly IDeactiveCustomer _deactiveCustomer;
    private readonly IUpdateCustomer _updateCustomer;
    private readonly ICustomerConsumptionHistory _customerConsumptionHistory;

    public CustomerController(
        IRegisterCustomer registerCustomer,
        IExistsCustomerByDocument existsCustomerByDocument,
        IGetByCustomerId getByCustomerId,
        IGetAllCustomers getAllCustomers,
        IDeactiveCustomer deactiveCustomer,
        IUpdateCustomer updateCustomer,
        ICustomerConsumptionHistory customerConsumptionHistory)
    {
        _registerCustomer = registerCustomer;
        _existsCustomerByDocument = existsCustomerByDocument;
        _getByCustomerId = getByCustomerId;
        _getAllCustomers = getAllCustomers;
        _deactiveCustomer = deactiveCustomer;
        _updateCustomer = updateCustomer;
        _customerConsumptionHistory = customerConsumptionHistory;
    }

    /// <summary>
    /// Cadastra um novo cliente.
    /// </summary>
    /// <param name="input">Dados do cliente e endereço.</param>
    /// <response code="201">Cliente criado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="409">Documento ou e-mail já cadastrado.</response>
    [Authorize]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterCustomerInput input)
    {
        var result = await _registerCustomer.Execute(input);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.ToApiResponse()
        );
    }

    /// <summary>
    /// Verifica se já existe cliente com o documento informado.
    /// </summary>
    /// <param name="document">CPF ou CNPJ do cliente.</param>
    /// <response code="200">Consulta realizada com sucesso.</response>
    /// <response code="400">Documento inválido.</response>
    /// <response code="401">Não autenticado.</response>
    [Authorize]
    [HttpGet("exists")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExistsByDocument([FromQuery] string document)
    {
        var result = await _existsCustomerByDocument.Execute(document);

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
    /// Obtém cliente por identificador.
    /// </summary>
    /// <param name="id">Identificador único do cliente.</param>
    /// <response code="200">Cliente encontrado.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Cliente não encontrado.</response>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getByCustomerId.Execute(id);

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
    /// Lista todos os clientes ativos.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="401">Não autenticado.</response>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CustomerResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _getAllCustomers.Execute();

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
    /// Consulta histórico de consumo do cliente no período informado.
    /// </summary>
    /// <param name="id">Identificador do cliente.</param>
    /// <param name="input">Período de consulta (data inicial e final).</param>
    /// <response code="200">Histórico retornado com sucesso.</response>
    /// <response code="400">Período inválido.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Cliente não encontrado.</response>
    [Authorize]
    [HttpPost("{id:guid}/consumption-history")]
    [ProducesResponseType(typeof(ApiResponse<CustomerConsumptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsumptionHistory(
        Guid id,
        [FromBody] CustomerConsumptionHistoryInput input)
    {
        var result = await _customerConsumptionHistory.Execute(id, input);

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
    /// Desativa um cliente (exclusão lógica).
    /// </summary>
    /// <remarks>Requer perfil <c>Manager</c>.</remarks>
    /// <param name="id">Identificador do cliente.</param>
    /// <response code="204">Cliente desativado com sucesso.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (apenas Manager).</response>
    /// <response code="404">Cliente não encontrado.</response>
    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactive(Guid id)
    {
        var result = await _deactiveCustomer.Execute(id);

        if (result.IsFailure)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        return NoContent();
    }

    /// <summary>
    /// Atualiza dados cadastrais do cliente.
    /// </summary>
    /// <param name="input">Campos a serem atualizados (parciais).</param>
    /// <param name="id">Identificador do cliente.</param>
    /// <response code="200">Cliente atualizado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Cliente não encontrado.</response>
    [Authorize]
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(UpdateCustomerInput input, Guid id)
    {
        var result = await _updateCustomer.Execute(input, id);

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
