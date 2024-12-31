using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Calendar.WPF.ViewModels;

public partial class DayViewModel : ObservableObject
{
    public required int Year { get; init; }

    public required int MonthNumber { get; init; }

    public required string MonthName { get; init; }

    public required int Number { get; init; }
    
    public required string Name { get; init; }

    public required string? Holiday { get; init; }

    public bool IsToday => new DateOnly(Year, MonthNumber, Number) == DateOnly.FromDateTime(DateTime.Today);
    
    [ObservableProperty]
    private ObservableCollection<ReminderViewModel>? _reminders;
    
    [ObservableProperty]
    private ObservableCollection<CalendarTaskViewModel>? _tasks;
}

public record ReminderViewModel(Guid Id, string Text, DateTimeOffset RemindAtUtc)
{
    public string RemindAtDisplay => RemindAtUtc.LocalDateTime.ToString("HH:mm");
}

public record CalendarTaskViewModel(Guid Id, string Title, string Description, DateTimeOffset RemindAtUtc)
{
    public string RemindAtDisplay => RemindAtUtc.LocalDateTime.ToString("HH:mm");
}
