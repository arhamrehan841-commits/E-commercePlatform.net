namespace SharedKernel.Database;

public interface IModuleDatabase
{
    Task MigrateAsync();
    Task SeedAsync();
}