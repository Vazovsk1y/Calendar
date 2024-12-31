using System.Globalization;
using System.Windows.Data;
using Calendar.WPF.ViewModels;

namespace Calendar.WPF.Infrastructure;

public class DayViewModelDaysConverter : IValueConverter
{
    private static readonly Dictionary<int, IReadOnlyCollection<int>> MonthDaysCountToDays = new()
    {
        { 30, Enumerable.Range(1, 30).ToList() },
        { 31, Enumerable.Range(1, 31).ToList() },
        { 28, Enumerable.Range(1, 28).ToList() },
        { 29, Enumerable.Range(1, 29).ToList() },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DayViewModel d)
        {
            return MonthDaysCountToDays[DateTime.DaysInMonth(d.Year, d.MonthNumber)];
        }
        
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}