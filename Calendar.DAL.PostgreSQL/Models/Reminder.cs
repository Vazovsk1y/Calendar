namespace Calendar.DAL.PostgreSQL.Models;

public class Reminder
{
    public required Guid Id { get; init; }
    
    public required DateTimeOffset RemindAt { get; init; }
    
    public required string Text { get; init; }
}