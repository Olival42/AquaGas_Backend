namespace AquaGas.Api.Modules.Sale.Web.Controllers;

using AquaGas.Product.Application.Repositories;
using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.UseCases;
using AquaGas.Shared.Http;
using AquaGas.Shared.Responses;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/sales")]
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

    [Authorize]
    [HttpPost("register")]
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

    [Authorize]
    [HttpGet("{Id}")]
    public async Task<IActionResult> GetById(Guid Id)
    {
        var result = await _getById.Execute(Id);

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

    [Authorize]
    [HttpPost("{Id}/cancel")]
    public async Task<IActionResult> Cancel(Guid Id, [FromBody] CancelSaleInput data)
    {
        var result = await _cancelSale.Execute(Id, data);

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
