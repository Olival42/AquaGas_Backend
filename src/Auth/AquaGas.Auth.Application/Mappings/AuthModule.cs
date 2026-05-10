using AquaGas.Auth.Application.Mappings;

namespace AquaGas.Auth.Application;

public static class AuthModule
{
    public static void Register()
    {
        UserMapping.Register();
    }
}