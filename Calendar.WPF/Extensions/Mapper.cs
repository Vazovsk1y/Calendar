using System.Globalization;
using Calendar.WPF.ViewModels;

namespace Calendar.WPF.Extensions;

public static class Mapper
{
    public static YearViewModel ToViewModel(this int year)
    {
        return new YearViewModel(
            year, Enumerable.Range(1, 12).Select(m =>
            {
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m);
                monthName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(monthName.ToLower());

                return new MonthViewModel(year, m, monthName, GetDaysInMonth(year, m, monthName));
            }).ToList());
    }
    
    private static List<DayViewModel> GetDaysInMonth(int year, int month, string monthName)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return Enumerable
            .Range(1, daysInMonth)
            .Select(day =>
            {
                _ = Constants.MonthAndDayToHoliday.TryGetValue((month, day), out var holiday);
                var dayName = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(new DateTime(year, month, day).DayOfWeek);
                
                return new DayViewModel
                {
                    Year = year,
                    MonthNumber = month,
                    MonthName = monthName,
                    Number = day,
                    Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dayName.ToLower()),
                    Holiday = holiday,
                };
            })
            .ToList();
    }
}