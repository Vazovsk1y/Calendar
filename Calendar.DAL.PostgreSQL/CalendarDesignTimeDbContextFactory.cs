using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Calendar.DAL.PostgreSQL;

public class CalendarDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CalendarDbContext>
{
    private const string ConnectionString = "User ID=postgres;Password=12345678;Host=localhost;Port=5432;Database=CalendarDb;";
    
    public CalendarDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CalendarDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString);
        return new CalendarDbContext(optionsBuilder.Options);
    }
}