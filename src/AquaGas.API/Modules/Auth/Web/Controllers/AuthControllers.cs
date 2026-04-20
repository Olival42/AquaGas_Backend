namespace AquaGas.Api.Modules.Auth.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AquaGas.Api.Modules.Auth.Application.UseCase;
using AquaGas.Api.Shared.Responses;
using AquaGas.Api.Modules.Auth.Application.Dtos.Requests;
using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Shared.Http;

[ApiController]
[Route("api/auth")]
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

    [AllowAnonymous]
    [HttpPost("login")]
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

    [AllowAnonymous]
    [HttpPost("logout")]
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

    [AllowAnonymous]
    [HttpPost("refresh")]
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