namespace BuildingBlocks.Database;

public interface IModuleDatabase
{
    Task MigrateAsync();
    Task SeedAsync();
}