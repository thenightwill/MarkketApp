namespace Infraestructure.Persistance.Seed;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminPassword { get; set; } = string.Empty;

    public string EmployeePassword { get; set; } = string.Empty;
}
