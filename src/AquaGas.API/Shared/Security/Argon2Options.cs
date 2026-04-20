namespace AquaGas.Api.Shared.Security;

public class Argon2Options
{
    public int TimeCost { get; set; } = 2;
    public int MemoryCost { get; set; } = 16384;
    public int Lanes { get; set; } = Environment.ProcessorCount;
}