namespace AquaGas.Employee.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AquaGas.Product.Application.UseCases;
using AquaGas.Product.Application.Dtos.Requests;
using AquaGas.Product.Application.Dtos.Responses;
using AquaGas.Shared.Http;
using AquaGas.Shared.Responses;
using AquaGas.Shared.OpenApi;

/// <summary>
/// Operações de cadastro, consulta, atualização de produtos e controle de estoque.
/// </summary>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
[Tags(ApiDocumentation.Tags.Products)]
public class ProductController : ControllerBase
{
    private readonly IRegisterProduct _registerProduct;
    private readonly IGetByProductId _getByProductId;
    private readonly IGetAllProducts _getAllProducts;
    private readonly IDeactiveProduct _deactiveProduct;
    private readonly IUpdateProduct _updateProduct;
    private readonly IUpdateStock _updateStock;

    public ProductController(
        IRegisterProduct registerProduct,
        IGetByProductId getByProductId,
        IGetAllProducts getAllProducts,
        IDeactiveProduct deactiveProduct,
        IUpdateProduct updateProduct,
        IUpdateStock updateStock)
    {
        _registerProduct = registerProduct;
        _getByProductId = getByProductId;
        _getAllProducts = getAllProducts;
        _deactiveProduct = deactiveProduct;
        _updateProduct = updateProduct;
        _updateStock = updateStock;
    }

    /// <summary>
    /// Cadastra um novo produto.
    /// </summary>
    /// <remarks>Requer perfil <c>Manager</c>.</remarks>
    /// <param name="input">Dados do produto.</param>
    /// <response code="201">Produto criado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (apenas Manager).</response>
    /// <response code="409">Produto já cadastrado.</response>
    [Authorize(Roles = "Manager")]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterProductInput input)
    {
        var result = await _registerProduct.Execute(input);

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
    /// Obtém produto por identificador.
    /// </summary>
    /// <param name="id">Identificador do produto.</param>
    /// <response code="200">Produto encontrado.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Produto não encontrado.</response>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getByProductId.Execute(id);

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
    /// Lista todos os produtos ativos.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="401">Não autenticado.</response>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _getAllProducts.Execute();

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
    /// Desativa um produto (exclusão lógica).
    /// </summary>
    /// <remarks>Requer perfil <c>Manager</c>.</remarks>
    /// <param name="id">Identificador do produto.</param>
    /// <response code="204">Produto desativado com sucesso.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (apenas Manager).</response>
    /// <response code="404">Produto não encontrado.</response>
    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactive(Guid id)
    {
        var result = await _deactiveProduct.Execute(id);

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
    /// Atualiza dados cadastrais do produto.
    /// </summary>
    /// <param name="input">Campos a serem atualizados.</param>
    /// <param name="id">Identificador do produto.</param>
    /// <response code="200">Produto atualizado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Produto não encontrado.</response>
    [Authorize]
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(UpdateProductInput input, Guid id)
    {
        var result = await _updateProduct.Execute(input, id);

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
    /// Atualiza estoque do produto (entrada ou saída).
    /// </summary>
    /// <param name="input">Tipo de movimentação e quantidade.</param>
    /// <param name="id">Identificador do produto.</param>
    /// <response code="200">Estoque atualizado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Produto não encontrado.</response>
    /// <response code="409">Estoque insuficiente.</response>
    [Authorize]
    [HttpPatch("{id:guid}/stock")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStock(UpdateStockInput input, Guid id)
    {
        var result = await _updateStock.Execute(input, id);

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
