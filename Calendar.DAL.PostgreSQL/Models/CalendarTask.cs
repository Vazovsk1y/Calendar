namespace Calendar.DAL.PostgreSQL.Models;

// Named as `CalendarTask` with purpose not be confused with `Task` class from TPL.
public class CalendarTask
{
    public required Guid Id { get; init; }
    
    public required DateTimeOffset RemindAt { get; init; }
    
    public required string Title { get; init; }
    
    public required string Description { get; init; }
}