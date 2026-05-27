namespace AquaGas.Api.Modules.Employee.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AquaGas.Employee.Application.UseCases;
using AquaGas.Employee.Application.Dtos.Requests;
using AquaGas.Employee.Application.Dtos.Responses;
using AquaGas.Shared.Http;
using AquaGas.Shared.Responses;
using AquaGas.Shared.OpenApi;

/// <summary>
/// Operações de cadastro, consulta, atualização e desativação de funcionários.
/// </summary>
[ApiController]
[Route("api/employees")]
[Produces("application/json")]
[Tags(ApiDocumentation.Tags.Employees)]
public class EmployeeController : ControllerBase
{
    private readonly IRegisterEmployee _registerEmployee;
    private readonly IGetByIdEmployee _getByIdEmployee;
    private readonly IGetAllEmployees _getAllEmployees;
    private readonly IDeactiveEmployee _deactiveEmployee;
    private readonly IUpdateEmployee _updateEmployee;

    public EmployeeController(
        IRegisterEmployee registerEmployee,
        IGetByIdEmployee getByIdEmployee,
        IGetAllEmployees getAllEmployees,
        IDeactiveEmployee deactiveEmployee,
        IUpdateEmployee updateEmployee)
    {
        _registerEmployee = registerEmployee;
        _getByIdEmployee = getByIdEmployee;
        _getAllEmployees = getAllEmployees;
        _deactiveEmployee = deactiveEmployee;
        _updateEmployee = updateEmployee;
    }

    /// <summary>
    /// Cadastra um novo funcionário e usuário de acesso.
    /// </summary>
    /// <remarks>Requer perfil <c>Manager</c>.</remarks>
    /// <param name="input">Dados do funcionário e credenciais de usuário.</param>
    /// <response code="201">Funcionário criado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (apenas Manager).</response>
    /// <response code="409">Usuário ou documento já existente.</response>
    [Authorize(Roles = "Manager")]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeWithUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
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

    /// <summary>
    /// Obtém funcionário por identificador.
    /// </summary>
    /// <param name="id">Identificador do funcionário.</param>
    /// <response code="200">Funcionário encontrado.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="404">Funcionário não encontrado.</response>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeWithUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getByIdEmployee.Execute(id);

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
    /// Lista todos os funcionários ativos.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="401">Não autenticado.</response>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeeWithUserResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Desativa um funcionário (exclusão lógica).
    /// </summary>
    /// <remarks>Requer perfil <c>Manager</c>.</remarks>
    /// <param name="id">Identificador do funcionário.</param>
    /// <response code="204">Funcionário desativado com sucesso.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (apenas Manager).</response>
    /// <response code="404">Funcionário não encontrado.</response>
    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactive(Guid id)
    {
        var result = await _deactiveEmployee.Execute(id);

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
    /// Atualiza dados do funcionário e usuário associado.
    /// </summary>
    /// <remarks>Requer perfil <c>Manager</c>.</remarks>
    /// <param name="input">Campos a serem atualizados.</param>
    /// <param name="id">Identificador do funcionário.</param>
    /// <response code="200">Funcionário atualizado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (apenas Manager).</response>
    /// <response code="404">Funcionário não encontrado.</response>
    [Authorize(Roles = "Manager")]
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeWithUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(UpdateEmployeeInput input, Guid id)
    {
        var result = await _updateEmployee.Execute(input, id);

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
