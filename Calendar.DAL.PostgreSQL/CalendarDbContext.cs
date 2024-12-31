using Calendar.DAL.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.DAL.PostgreSQL;

public class CalendarDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<CalendarTask> Tasks { get; init; }
    
    public DbSet<Reminder> Reminders { get; init; }
}