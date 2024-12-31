namespace Calendar.WPF.ViewModels;

public record MonthViewModel(int Year, int Number, string Name, IReadOnlyCollection<DayViewModel> Days);