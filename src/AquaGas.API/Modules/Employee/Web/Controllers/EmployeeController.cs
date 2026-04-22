namespace AquaGas.Api.Modules.Employee.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AquaGas.Api.Modules.Employee.Application.UseCases;

using AquaGas.Api.Shared.Http;
using AquaGas.Api.Shared.Responses;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;


[ApiController]
[Route("api/employees")]
public class EmployeeController : ControllerBase
{
    private readonly IRegisterEmployee _registerEmployee;
    private readonly IGetByIdEmployee _getByIdEmployee;
    private readonly IGetAllEmployees _getAllEmployees;
    private readonly IDeactiveEmployee _deactiveEmployee;
    private readonly IUpdateEmployee _updateEmployee;
    private readonly IResetPassword _resetPassword;

    public EmployeeController(
        IRegisterEmployee registerEmployee,
        IGetByIdEmployee getByIdEmployee,
        IGetAllEmployees getAllEmployees,
        IDeactiveEmployee deactiveEmployee,
        IUpdateEmployee updateEmployee,
        IResetPassword resetPassword)
    {
        _registerEmployee = registerEmployee;
        _getByIdEmployee = getByIdEmployee;
        _getAllEmployees = getAllEmployees;
        _deactiveEmployee = deactiveEmployee;
        _updateEmployee = updateEmployee;
        _resetPassword = resetPassword;
    }

    [Authorize]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterEmployeeInput input)
    {
        var result = await _registerEmployee.Execute(input);

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
            new { id = result.Value!.Employee.Id },
            result.ToApiResponse()
        );
    }

    [Authorize]
    [HttpGet("{Id}")]
    public async Task<IActionResult> GetById(Guid Id)
    {
        var result = await _getByIdEmployee.Execute(Id);

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
        var result = await _getAllEmployees.Execute();

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
        var result = await _deactiveEmployee.Execute(Id);

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

    [Authorize(Roles = "Manager")]
    [HttpPatch("{Id}")]
    public async Task<IActionResult> Update(UpdateEmployeeInput input, Guid Id)
    {
        var result = await _updateEmployee.Execute(input, Id);

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
    [HttpPatch("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordInput input)
    {
        var result = await _resetPassword.Execute(id, input);

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
}