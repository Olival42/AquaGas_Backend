namespace AquaGas.Api.Modules.Auth.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AquaGas.Auth.Application.UseCase;
using AquaGas.Auth.Application.Dtos.Requests;
using AquaGas.Shared.Responses;
using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Shared.Http;
using AquaGas.Shared.OpenApi;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Endpoints de autenticação, renovação de token e encerramento de sessão.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags(ApiDocumentation.Tags.Auth)]
public class AuthController : ControllerBase
{
    private readonly ILogin _loginUseCase;
    private readonly ILogout _logoutUseCase;
    private readonly IRefresh _refreshUseCase;

    public AuthController(ILogin loginUseCase, ILogout logoutUseCase, IRefresh refreshUseCase)
    {
        _loginUseCase = loginUseCase;
        _logoutUseCase = logoutUseCase;
        _refreshUseCase = refreshUseCase;
    }

    /// <summary>
    /// Realiza login do usuário.
    /// </summary>
    /// <remarks>
    /// Valida credenciais e retorna o access token JWT.
    /// O refresh token é gravado automaticamente em cookie HttpOnly (`refreshToken`).
    /// </remarks>
    /// <param name="request">Credenciais de acesso (usuário e senha).</param>
    /// <response code="200">Login realizado com sucesso.</response>
    /// <response code="400">Credenciais inválidas ou falha de validação.</response>
    /// <response code="401">Usuário ou senha incorretos.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginInput request)
    {
        var result = await _loginUseCase.Execute(request);

        if (!result.IsSuccess)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        SetRefreshCookie(
            result.Value!.Tokens.RefreshToken,
            result.Value.Tokens.RefreshExpiresAt);

        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
        (
            result.Value.User,
            result.Value.Tokens.AccessToken,
            result.Value.Tokens.AccessExpiresAt
        )));
    }

    /// <summary>
    /// Encerra a sessão do usuário.
    /// </summary>
    /// <remarks>
    /// Invalida o refresh token (cookie) e o access token informado no header Authorization, quando presente.
    /// </remarks>
    /// <response code="204">Logout concluído com sucesso.</response>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        var accessToken = HttpContext.Request.Headers["Authorization"]
            .FirstOrDefault()?
            .Replace("Bearer ", "")
            .Trim();

        await _logoutUseCase.Execute(refreshToken ?? "", accessToken ?? "");

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });

        return NoContent();
    }

    /// <summary>
    /// Renova o access token JWT.
    /// </summary>
    /// <remarks>
    /// Utiliza o refresh token armazenado no cookie HttpOnly (`refreshToken`) e retorna um novo access token.
    /// </remarks>
    /// <response code="200">Token renovado com sucesso.</response>
    /// <response code="401">Refresh token ausente, inválido ou expirado.</response>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized();

        var result = await _refreshUseCase.Execute(refreshToken);

        if (!result.IsSuccess)
        {
            var firstError = result.Errors.First();

            return ErrorResponseHelper.ToActionResult(
                firstError.Code,
                result.ToApiResponse()
            );
        }

        SetRefreshCookie(
            result.Value!.Tokens.RefreshToken,
            result.Value.Tokens.RefreshExpiresAt);

        return Ok(ApiResponse<RefreshResponse>.Ok(new RefreshResponse
        (
            result.Value.Tokens.AccessToken,
            result.Value.Tokens.AccessExpiresAt
        )));
    }

    private void SetRefreshCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append(
            "refreshToken",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
            }
        );
    }
}
