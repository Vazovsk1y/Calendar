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

public partial class CalendarTaskAddViewModel : ObservableRecipient
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private DateTime _date;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private DateTime _time;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _title;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _description;

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
        var task = new CalendarTask
        {
            Id = Guid.CreateVersion7(),
            RemindAt = combinedDate.ToUniversalTime(),
            Title = Title!,
            Description = Description!,
        };
        
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();

        Messenger.Send(new CalendarTaskAddedMessage(combinedDate.Year, combinedDate.Month, combinedDate.Day));
        Messenger.Send(new CloseDialogRequest());
        snackbarMessageQueue.Enqueue("Новая задача добавлена.");
        Scheduler.Schedule(() => trayIcon.ShowBalloonTip(task.Title, task.Description, BalloonIcon.Info), task.RemindAt - DateTimeOffset.UtcNow, task.Id);
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(Title) &&
                                 !string.IsNullOrWhiteSpace(Description) &&
                                 Date >= DateTime.Today;

    [RelayCommand]
    private void Close()
    {
        Messenger.Send(new CloseDialogRequest());
    }
}