namespace Calendar.WPF.ViewModels;

public record YearViewModel(int Number, IReadOnlyCollection<MonthViewModel> Months);