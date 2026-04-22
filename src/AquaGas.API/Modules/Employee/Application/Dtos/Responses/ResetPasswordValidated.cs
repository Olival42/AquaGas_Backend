namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public record ResetPasswordValidated(
    Password Password
);