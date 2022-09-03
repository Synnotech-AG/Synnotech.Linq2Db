using LinqToDB.Mapping;

namespace Synnotech.Linq2Db.MsSqlServer.Tests;

public static class DatabaseMappings
{
    public static void CreateMappings(MappingSchema mappingSchema)
    {
        var builder = mappingSchema.GetFluentMappingBuilder();

        builder.Entity<Employee>()
               .HasTableName("Employees")
               .Property(e => e.Id).IsIdentity().IsPrimaryKey()
               .Property(e => e.Name).HasLength(50).IsNullable(false)
               .Property(e => e.Age).IsNullable(false);
    }
}