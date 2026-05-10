using AquaGas.Employee.Application.Mappings;

namespace AquaGas.Employee.Application;

public static class EmployeeModule
{
    public static void RegisterMappings()
    {
        EmployeeMapping.Register();
    }
}