using Calendar.DAL.PostgreSQL;
using Calendar.DAL.PostgreSQL.Models;
using Calendar.WPF.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hardcodet.Wpf.TaskbarNotification;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace Calendar.WPF.ViewModels;

public partial class ReminderAddViewModel : ObservableRecipient
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private DateTime _date;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private DateTime _time;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _text;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task Confirm()
    {
        if (ConfirmCommand.IsRunning || 
            (Date == DateTime.Today && Time.TimeOfDay <= DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(1))))
        {
            return;
        }

        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
        var snackbarMessageQueue = scope.ServiceProvider.GetRequiredService<ISnackbarMessageQueue>();
        var trayIcon = scope.ServiceProvider.GetRequiredService<TaskbarIcon>();

        var combinedDate = new DateTime(Date.Year, Date.Month, Date.Day, Time.Hour, Time.Minute, Time.Second, DateTimeKind.Local);
        var reminder = new Reminder
        {
            Id = Guid.CreateVersion7(),
            RemindAt = combinedDate.ToUniversalTime(),
            Text = Text!,
        };
        
        dbContext.Reminders.Add(reminder);
        await dbContext.SaveChangesAsync();

        Messenger.Send(new ReminderAddedMessage(combinedDate.Year, combinedDate.Month, combinedDate.Day));
        Messenger.Send(new CloseDialogRequest());
        snackbarMessageQueue.Enqueue("Напоминание успешно добавлено.");
        Scheduler.Schedule(() => trayIcon.ShowBalloonTip("Напоминание", reminder.Text, BalloonIcon.Info), reminder.RemindAt - DateTimeOffset.UtcNow, reminder.Id);
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(Text) &&
                                 Date >= DateTime.Today;

    [RelayCommand]
    private void Close()
    {
        Messenger.Send(new CloseDialogRequest());
    }
}