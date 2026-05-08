namespace AquaGas.Api.Modules.Employee.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Shared.Http;
using AquaGas.Api.Shared.Responses;

[ApiController]
[Route("api/products")]
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

    [Authorize(Roles = "Manager")]
    [HttpPost("register")]
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

    [Authorize]
    [HttpGet("{Id}")]
    public async Task<IActionResult> GetById(Guid Id)
    {
        var result = await _getByProductId.Execute(Id);

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

    [Authorize]
    [HttpGet]
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

    [Authorize(Roles = "Manager")]
    [HttpDelete("{Id}")]
    public async Task<IActionResult> Deactive(Guid Id)
    {
        var result = await _deactiveProduct.Execute(Id);

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

    [Authorize]
    [HttpPatch("{Id}")]
    public async Task<IActionResult> Update(UpdateProductInput input, Guid Id)
    {
        var result = await _updateProduct.Execute(input, Id);

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

    [Authorize]
    [HttpPatch("{Id}/stock")]
    public async Task<IActionResult> UpdateStock(UpdateStockInput input, Guid Id)
    {
        var result = await _updateStock.Execute(input, Id);

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