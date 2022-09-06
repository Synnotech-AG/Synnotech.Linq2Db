using LinqToDB.Mapping;

namespace Synnotech.Linq2Db.MsSqlServer.Tests;

public static class DatabaseMappings
{
    public static MappingSchema CreateMappings()
    {
        var mappingSchema = new MappingSchema();
        var builder = mappingSchema.GetFluentMappingBuilder();

        builder.Entity<Employee>()
               .HasTableName("Employees")
               .Property(e => e.Id).IsIdentity().IsPrimaryKey()
               .Property(e => e.Name).HasLength(50).IsNullable(false)
               .Property(e => e.Age).IsNullable(false);

        return mappingSchema;
    }
}