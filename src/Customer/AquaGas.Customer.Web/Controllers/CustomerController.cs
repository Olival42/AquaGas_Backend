namespace AquaGas.Api.Modules.Customer.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AquaGas.Customer.Application.UseCases;
using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Shared.Http;
using AquaGas.Shared.Responses;

[ApiController]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly IRegisterCustomer _registerCustomer;
    private readonly IExistsCustomerByDocument _existsCustomerByDocument;
    private readonly IGetByCustomerId _getByCustomerId;
    private readonly IGetAllCustomers _getAllCustomers;
    private readonly IDeactiveCustomer _deactiveCustomer;
    private readonly IUpdateCustomer _updateCustomer;

    public CustomerController(
        IRegisterCustomer registerCustomer,
        IExistsCustomerByDocument existsCustomerByDocument,
        IGetByCustomerId getByCustomerId,
        IGetAllCustomers getAllCustomers,
        IDeactiveCustomer deactiveCustomer,
        IUpdateCustomer updateCustomer)
    {
        _registerCustomer = registerCustomer;
        _existsCustomerByDocument = existsCustomerByDocument;
        _getByCustomerId = getByCustomerId;
        _getAllCustomers = getAllCustomers;
        _deactiveCustomer = deactiveCustomer;
        _updateCustomer = updateCustomer;
    }

    [Authorize]
    [HttpPost("register")]
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

    [Authorize]
    [HttpGet("exists")]
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

    [Authorize]
    [HttpGet("{Id}")]
    public async Task<IActionResult> GetById(Guid Id)
    {
        var result = await _getByCustomerId.Execute(Id);

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

    [Authorize(Roles = "Manager")]
    [HttpDelete("{Id}")]
    public async Task<IActionResult> Deactive(Guid Id)
    {
        var result = await _deactiveCustomer.Execute(Id);

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
    public async Task<IActionResult> Update(UpdateCustomerInput input, Guid Id)
    {
        var result = await _updateCustomer.Execute(input, Id);

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
