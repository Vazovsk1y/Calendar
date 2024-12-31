using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows;
using Calendar.DAL.PostgreSQL;
using Calendar.WPF.Extensions;
using Calendar.WPF.Infrastructure;
using Calendar.WPF.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Calendar.WPF.ViewModels;

public partial class MainWindowViewModel : ObservableRecipient, 
    IRecipient<CloseDialogRequest>,
    IRecipient<ReminderAddedMessage>,
    IRecipient<CalendarTaskAddedMessage>
{
    public static readonly IEnumerable<EnumViewModel<DisplayType>> DisplayTypes = Enum
        .GetValues<DisplayType>()
        .Select(t => new EnumViewModel<DisplayType>(t))
        .ToList();

    public const string DeleteMessageBoxId = nameof(DeleteMessageBoxId);

    private readonly List<YearViewModel> _years;
    
    public IEnumerable<int> YearIntegers { get; }
    
    public IEnumerable<string> MonthStrings { get; }
    
    public ISnackbarMessageQueue SnackbarMessageQueue { get; }

    [ObservableProperty] 
    private object _selectedItem;

    [ObservableProperty] 
    private EnumViewModel<DisplayType> _selectedDisplayType;
    
    [ObservableProperty]
    private int _selectedYear;
    
    [ObservableProperty]
    private string? _selectedMonthName;

    [ObservableProperty]
    private int? _selectedDayNumber;

    [ObservableProperty]
    private ReminderAddViewModel? _reminderAddViewModel;

    [ObservableProperty] 
    private CalendarTaskAddViewModel? _calendarTaskAddViewModel;
    
    private DateOnly _selectedDate;
    
    public MainWindowViewModel(ISnackbarMessageQueue snackbarMessageQueue)
    {
        SnackbarMessageQueue = snackbarMessageQueue;
        IsActive = true;
        
        const int startYear = 1970;
        var endYear = DateTime.Now.Year + 5;

        var years = Enumerable
            .Range(startYear, endYear - startYear + 1)
            .Select(e => e.ToViewModel())
            .ToList();

        
        _years = years;
        YearIntegers = years.Select(e => e.Number).ToList();
        MonthStrings = years.First().Months.Select(e => e.Name).ToList();
        SelectedDisplayType = DisplayTypes.First();

        var currentYear = DateTime.Today.Year;
        
        _selectedDate = DateOnly.FromDateTime(DateTime.Today);
        SelectedItem =  years.First(e => e.Number == currentYear);
        SelectedYear = currentYear;
    }
    
    partial void OnSelectedMonthNameChanged(string? value)
    {
        if (CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(_selectedDate.Month).Equals(value, StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        switch (SelectedDisplayType.Value)
        {
            case DisplayType.Month:
            {
                var month = _years.First(y => y.Number == _selectedDate.Year).Months.First(e => e.Name == value);
                SelectedItem = month;
                _selectedDate = new DateOnly(month.Year, month.Number, 1);
                return;
            }
            case DisplayType.Day:
            {
                var year = _years.First(y => y.Number == _selectedDate.Year);
                var month = year.Months.First(m => m.Name == value);
                
                var day = month.Days.FirstOrDefault(e => e.Number == _selectedDate.Day);
                if (day is null)
                {
                    _selectedDate = new DateOnly(year.Number, month.Number, 1);
                    SelectedDisplayType = new EnumViewModel<DisplayType>(DisplayType.Month);
                    return;
                }
                
                SelectedItem = day;
                _selectedDate = new DateOnly(day.Year, day.MonthNumber, day.Number);
                LoadDayData(day);
                return;
            }
            case DisplayType.Year:
            default:
            {
                throw new InvalidOperationException();
            }
        }
    }
    
    [RelayCommand]
    private void MoveNext()
    {
        switch (SelectedDisplayType.Value)
        {
            case DisplayType.Year:
            {
                var selectedYear = (YearViewModel)SelectedItem;
                var index = _years.IndexOf(selectedYear);

                if (index == -1 || index == _years.Count - 1)
                {
                    return;
                }

                var nextYear = _years[++index];
                SelectedItem = nextYear;
                _selectedDate = new DateOnly(nextYear.Number, _selectedDate.Month, 1);
                SelectedYear = nextYear.Number;
                return;
            }
            case DisplayType.Month:
            {
                var selectedMonth = (MonthViewModel)SelectedItem;
                var selectedMonthIndex = selectedMonth.Number - 1;

                MonthViewModel? nextMonth;
                if (selectedMonthIndex == 11)
                {
                    var month = _years.FirstOrDefault(e => e.Number == selectedMonth.Year + 1)?.Months.ElementAt(0);
                    if (month is null)
                    {
                        return;
                    }

                    nextMonth = month;
                }
                else
                {
                    nextMonth = _years.First(e => e.Number == selectedMonth.Year).Months.ElementAt(++selectedMonthIndex);
                }

                SelectedItem = nextMonth;
                _selectedDate = new DateOnly(nextMonth.Year, nextMonth.Number, 1);
                SelectedYear = nextMonth.Year;
                SelectedMonthName = nextMonth.Name;
                return;
            }
            case DisplayType.Day:
            {
                var selectedDay = (DayViewModel)SelectedItem;

                var year = _years.First(e => e.Number == selectedDay.Year);
                var month = year.Months.First(m => m.Number == selectedDay.MonthNumber);

                var dayIndex = selectedDay.Number - 1;

                DayViewModel? nextDay;
                if (dayIndex < month.Days.Count - 1)
                {
                    nextDay = month.Days.ElementAt(++dayIndex);
                }
                else
                {
                    var nextMonth = year.Months.FirstOrDefault(m => m.Number == selectedDay.MonthNumber + 1);
                    if (nextMonth is not null)
                    {
                        nextDay = nextMonth.Days.First();
                    }
                    else
                    {
                        var nextYear = _years.FirstOrDefault(e => e.Number == selectedDay.Year + 1);
                        if (nextYear is null)
                        {
                            return;
                        }

                        var firstMonth = nextYear.Months.First();
                        nextDay = firstMonth.Days.First();
                    }
                }

                SelectedItem = nextDay;
                _selectedDate = new DateOnly(nextDay.Year, nextDay.MonthNumber, nextDay.Number);
                SelectedYear = nextDay.Year;
                SelectedMonthName = nextDay.MonthName;
                SelectedDayNumber = nextDay.Number;
                LoadDayData(nextDay);
                return;
            }
            default:
                throw new InvalidOperationException();
        }
    }

    [RelayCommand]
    private void MovePrevious()
    {
        switch (SelectedDisplayType.Value)
        {
            case DisplayType.Year:
            {
                var selectedYear = (YearViewModel)SelectedItem;
                var index = _years.IndexOf(selectedYear);

                if (index <= 0)
                {
                    return;
                }

                var previousYear = _years[--index];
                SelectedItem = previousYear;
                _selectedDate = new DateOnly(previousYear.Number, _selectedDate.Month, 1);
                SelectedYear = previousYear.Number;
                return;
            }
            case DisplayType.Month:
            {
                var selectedMonth = (MonthViewModel)SelectedItem;
                var selectedMonthIndex = selectedMonth.Number - 1;

                MonthViewModel? previousMonth;
                if (selectedMonthIndex == 0)
                {
                    var previousYear = _years.FirstOrDefault(e => e.Number == selectedMonth.Year - 1);
                    if (previousYear is null)
                    {
                        return;
                    }

                    previousMonth = previousYear.Months.Last();
                }
                else
                {
                    previousMonth = _years.First(e => e.Number == selectedMonth.Year).Months
                        .ElementAt(--selectedMonthIndex);
                }

                SelectedItem = previousMonth;
                _selectedDate = new DateOnly(previousMonth.Year, previousMonth.Number, 1);
                SelectedYear = previousMonth.Year;
                SelectedMonthName = previousMonth.Name;
                return;
            }
            case DisplayType.Day:
            {
                var selectedDay = (DayViewModel)SelectedItem;

                var year = _years.First(e => e.Number == selectedDay.Year);
                var month = year.Months.First(m => m.Number == selectedDay.MonthNumber);

                var dayIndex = selectedDay.Number - 1;

                DayViewModel? previousDay;
                if (dayIndex > 0)
                {
                    previousDay = month.Days.ElementAt(dayIndex - 1);
                }
                else
                {
                    var previousMonth = year.Months.FirstOrDefault(m => m.Number == selectedDay.MonthNumber - 1);
                    if (previousMonth is not null)
                    {
                        previousDay = previousMonth.Days.Last();
                    }
                    else
                    {
                        var previousYear = _years.FirstOrDefault(e => e.Number == selectedDay.Year - 1);
                        if (previousYear is null)
                        {
                            return;
                        }

                        var lastMonth = previousYear.Months.Last();
                        previousDay = lastMonth.Days.Last();
                    }
                }

                SelectedItem = previousDay;
                _selectedDate = new DateOnly(previousDay.Year, previousDay.MonthNumber, previousDay.Number);
                SelectedYear = previousDay.Year;
                SelectedMonthName = previousDay.MonthName;
                SelectedDayNumber = previousDay.Number;
                LoadDayData(previousDay);
                return;
            }
            default:
                throw new InvalidOperationException();
        }
    }
    
    public void Receive(CloseDialogRequest message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ReminderAddViewModel = null;
            CalendarTaskAddViewModel = null;
        });
    }

    public void Receive(ReminderAddedMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var target = _years.First(e => e.Number == message.Year)
                .Months.First(e => e.Number == message.Month)
                .Days.First(e => e.Number == message.Day);

            target.Reminders = null;
            if (ReferenceEquals(target, SelectedItem))
            {
                LoadDayData(target);
            }
        });
    }
    
    public void Receive(CalendarTaskAddedMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var target = _years.First(e => e.Number == message.Year)
                .Months.First(e => e.Number == message.Month)
                .Days.First(e => e.Number == message.Day);

            target.Tasks = null;
            if (ReferenceEquals(target, SelectedItem))
            {
                LoadDayData(target);
            }
        });
    }

    [RelayCommand]
    private void OpenReminderAddDialog()
    {
        var vm = new ReminderAddViewModel
        {
            Date = DateTime.Today.Add(TimeSpan.FromDays(1)),
            Time = DateTime.Now,
        };
        
        ReminderAddViewModel = vm;
    }

    [RelayCommand]
    private void OpenCalendarTaskAddDialog()
    {
        var vm = new CalendarTaskAddViewModel
        {
            Date = DateTime.Today.Add(TimeSpan.FromDays(1)),
            Time = DateTime.Now,
        };
        
        CalendarTaskAddViewModel = vm;
    }

    [RelayCommand]
    private async Task DeleteReminder(ReminderViewModel vm)
    {
        var mb = new OkCancelMessageBoxControl
        {
            DataContext = new MessageBoxViewModel
            {
                Message = "Вы уверены, что желаете удалить данное напоминание?",
            }
        };
        
        var result = await DialogHost.Show(mb, DeleteMessageBoxId);

        if (result is true)
        {
            using var scope = App.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();

            var rCount = await dbContext
                .Reminders
                .Where(e => e.Id == vm.Id)
                .ExecuteDeleteAsync();

            if (rCount > 0)
            {
                var selectedDay = SelectedItem as DayViewModel;
                selectedDay?.Reminders?.Remove(vm);
                Scheduler.Cancel(vm.Id);
            }
        }
    }
    
    [RelayCommand]
    private async Task DeleteCalendarTask(CalendarTaskViewModel vm)
    {
        var mb = new OkCancelMessageBoxControl
        {
            DataContext = new MessageBoxViewModel
            {
                Message = "Вы уверены, что желаете удалить данную задачу?",
            }
        };
        
        var result = await DialogHost.Show(mb, DeleteMessageBoxId);

        if (result is true)
        {
            using var scope = App.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();

            var rCount = await dbContext
                .Tasks
                .Where(e => e.Id == vm.Id)
                .ExecuteDeleteAsync();

            if (rCount > 0)
            {
                var selectedDay = SelectedItem as DayViewModel;
                selectedDay?.Tasks?.Remove(vm);
                Scheduler.Cancel(vm.Id);
            }
        }
    }
    
    [RelayCommand]
    private void MoveToToday()
    {
        var (currentYear, currentMonth, currentDay) = DateTime.Now;
        _selectedDate = new DateOnly(currentYear, currentMonth, currentDay);

        switch (SelectedDisplayType.Value)
        {
            case DisplayType.Year:
            {
                var selectedYear = _years.First(y => y.Number == currentYear);
                SelectedItem = selectedYear;
                SelectedYear = selectedYear.Number;
                break;
            }
            case DisplayType.Month:
            {
                var selectedYear = _years.First(y => y.Number == currentYear);
                var selectedMonth = selectedYear.Months.First(m => m.Number == currentMonth);
                SelectedItem = selectedMonth;
                SelectedYear = selectedYear.Number;
                SelectedMonthName = selectedMonth.Name;
                break;
            }
            case DisplayType.Day:
            {
                var selectedYear = _years.First(y => y.Number == currentYear);
                var selectedMonth = selectedYear.Months.First(m => m.Number == currentMonth);
                var selectedDay = selectedMonth.Days.First(d => d.Number == currentDay);
                SelectedItem = selectedDay;
                SelectedYear = selectedYear.Number;
                SelectedMonthName = selectedMonth.Name;
                SelectedDayNumber = selectedDay.Number;
                LoadDayData(selectedDay);
                break;
            }
            default:
                throw new InvalidOperationException();
        }
    }

    [RelayCommand]
    private void OnItemSelected(object? item)
    {
        if (item is null)
        {
            return;
        }

        switch (item)
        {
            case MonthViewModel month:
            {
                _selectedDate = new DateOnly(month.Year, month.Number, 1);
                SelectedItem = month;
                SelectedDisplayType = new EnumViewModel<DisplayType>(DisplayType.Month);
                SelectedYear = month.Year;
                SelectedMonthName = month.Name;
                return;
            }
            case DayViewModel day:
            {
                _selectedDate = new DateOnly(day.Year, day.MonthNumber, day.Number);
                SelectedItem = day;
                SelectedDisplayType = new EnumViewModel<DisplayType>(DisplayType.Day);
                SelectedYear = day.Year;
                SelectedMonthName = day.MonthName;
                SelectedDayNumber = day.Number;
                LoadDayData(day);
                return;
            }
            default:
                throw new InvalidOperationException();
        }
    }

    partial void OnSelectedDayNumberChanged(int? value)
    {
        if (value is null)
        {
            return;
        }
        
        if (_selectedDate.Day == value)
        {
            return;
        }
        
        var day = _years.First(e => e.Number == _selectedDate.Year)
            .Months.First(e => e.Number == _selectedDate.Month)
            .Days.First(d => d.Number == value);
        SelectedItem = day;
        
        _selectedDate = new DateOnly(_selectedDate.Year, _selectedDate.Month, (int)value);
        LoadDayData(day);
    }

    partial void OnSelectedDisplayTypeChanged(EnumViewModel<DisplayType>? oldValue, EnumViewModel<DisplayType> newValue)
    {
        if (oldValue is null)
        {
            return;
        }

        switch (newValue.Value)
        {
            case DisplayType.Year:
            {
                if (SelectedItem is YearViewModel)
                {
                    return;
                }

                SelectedItem = _years.First(y => y.Number == _selectedDate.Year);
                return;
            }
            case DisplayType.Month:
            {
                if (SelectedItem is MonthViewModel)
                {
                    return;
                }

                var month = _years.First(e => e.Number == _selectedDate.Year)
                    .Months
                    .First(e => e.Number == _selectedDate.Month);
                
                SelectedItem = month;
                SelectedYear = month.Year;
                SelectedMonthName = month.Name;
                return;
            }
            case DisplayType.Day:
            {
                if (SelectedItem is DayViewModel)
                {
                    return;
                }

                var day = _years.First(e => e.Number == _selectedDate.Year)
                    .Months.First(e => e.Number == _selectedDate.Month)
                    .Days.First();
                
                SelectedItem = day;
                _selectedDate = new DateOnly(day.Year, day.MonthNumber, day.Number);
                SelectedYear = day.Year;
                SelectedMonthName = day.MonthName;
                SelectedDayNumber = day.Number;
                LoadDayData(day);
                return;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    partial void OnSelectedYearChanged(int value)
    {
        if (_selectedDate.Year == value)
        {
            return;
        }

        switch (SelectedDisplayType.Value)
        {
            case DisplayType.Year:
            {
                var year = _years.First(y => y.Number == value);
                SelectedItem = year;
                _selectedDate = new DateOnly(year.Number, _selectedDate.Month, 1);
                return;
            }
            case DisplayType.Month:
            {
                var year = _years.First(y => y.Number == value);
                var month = year.Months.First(m => m.Number == _selectedDate.Month);
                SelectedItem = month;
                _selectedDate = new DateOnly(year.Number, month.Number, 1);
                return;
            }
            case DisplayType.Day:
            {
                var year = _years.First(y => y.Number == value);
                var month = year.Months.First(m => m.Number == _selectedDate.Month);
                
                var day = month.Days.FirstOrDefault(e => e.Number == _selectedDate.Day);
                if (day is null)
                {
                    _selectedDate = new DateOnly(year.Number, month.Number, 1);
                    SelectedDisplayType = new EnumViewModel<DisplayType>(DisplayType.Month);
                    return;
                }
                
                SelectedItem = day;
                _selectedDate = new DateOnly(day.Year, day.MonthNumber, day.Number);
                LoadDayData(day);
                return;
            }
            default:
                throw new InvalidOperationException();
        }
    }

    private static void LoadDayData(DayViewModel day)
    {
        if (day.Tasks is not null && day.Reminders is not null)
        {
            return;
        }
        
        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();

        var localDate = new DateTime(day.Year, day.MonthNumber, day.Number);

        var startOfDay = localDate.Date;
        var endOfDay = localDate.Date.AddDays(1);

        var startOfDayUtc = startOfDay.ToUniversalTime();
        var endOfDayUtc = endOfDay.ToUniversalTime();

        day.Reminders = new ObservableCollection<ReminderViewModel>(dbContext
            .Reminders
            .Where(e => e.RemindAt >= startOfDayUtc && e.RemindAt < endOfDayUtc)
            .OrderBy(e => e.RemindAt)
            .Select(e => new ReminderViewModel(e.Id, e.Text, e.RemindAt)));

        day.Tasks = new ObservableCollection<CalendarTaskViewModel>(dbContext
            .Tasks
            .Where(t => t.RemindAt >= startOfDayUtc && t.RemindAt < endOfDayUtc)
            .OrderBy(e => e.RemindAt)
            .Select(e => new CalendarTaskViewModel(e.Id, e.Title, e.Description, e.RemindAt)));
    }
}

public enum DisplayType
{
    [Display(Name = "Год")] 
    Year,

    [Display(Name = "Месяц")] 
    Month,

    [Display(Name = "День")] 
    Day,
}