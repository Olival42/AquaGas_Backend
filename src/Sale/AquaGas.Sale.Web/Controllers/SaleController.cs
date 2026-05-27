namespace AquaGas.Api.Modules.Sale.Web.Controllers;

using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.Dtos.Responses;
using AquaGas.Sale.Application.UseCases;
using AquaGas.Shared.Http;
using AquaGas.Shared.OpenApi;
using AquaGas.Shared.Responses;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Operações de registro, consulta e cancelamento de vendas.
/// </summary>
[ApiController]
[Route("api/sales")]
[Produces("application/json")]
[Tags(ApiDocumentation.Tags.Sales)]
public sealed class SaleController : ControllerBase
{
    private readonly IRegisterSale _registerSale;
    private readonly IGetById _getById;
    private readonly IGetAllSales _getAllSales;
    private readonly ICancelSale _cancelSale;

    public SaleController(
        IRegisterSale registerSale,
        IGetById getById,
        IGetAllSales getAllSales,
        ICancelSale cancelSale)
    {
        _registerSale = registerSale;
        _getById = getById;
        _getAllSales = getAllSales;
        _cancelSale = cancelSale;
    }

    /// <summary>
    /// Registra uma nova venda.
    /// </summary>
    /// <param name="input">Itens da venda, cliente opcional e desconto.</param>
    /// <response code="201">Venda registrada com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Cliente ou produto não encontrado.</response>
    /// <response code="409">Estoque insuficiente ou conflito de regra de negócio.</response>
    [Authorize]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterSaleInput input)
    {
        var result = await _registerSale.Execute(input);

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
    /// Obtém venda por identificador.
    /// </summary>
    /// <param name="id">Identificador da venda.</param>
    /// <response code="200">Venda encontrada.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Venda não encontrada.</response>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status200OK)]
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
    /// Lista todas as vendas.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="401">Não autenticado.</response>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SaleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _getAllSales.Execute();

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
    /// Cancela uma venda existente.
    /// </summary>
    /// <param name="id">Identificador da venda.</param>
    /// <param name="data">Motivo e observações do cancelamento.</param>
    /// <response code="200">Venda cancelada com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Venda não encontrada.</response>
    /// <response code="409">Venda não pode ser cancelada no estado atual.</response>
    [Authorize]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<CancelSaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelSaleInput data)
    {
        var result = await _cancelSale.Execute(id, data);

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
