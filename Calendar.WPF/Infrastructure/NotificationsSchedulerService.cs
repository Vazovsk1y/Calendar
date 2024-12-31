using Calendar.DAL.PostgreSQL;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Calendar.WPF.Infrastructure;

public class NotificationsSchedulerService(
    IServiceScopeFactory scopeFactory, 
    TaskbarIcon trayIcon) : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromMinutes(10);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period);
        var fromOffset = TimeSpan.FromMinutes(1);
        
        do
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();

            try
            {
                var from = DateTimeOffset.UtcNow.Add(fromOffset);
                var to = from.Add(Period);

                var tasks = await dbContext
                    .Tasks
                    .AsNoTracking()
                    .Where(e => e.RemindAt >= from && e.RemindAt <= to)
                    .ToListAsync(stoppingToken);

                from = DateTimeOffset.UtcNow.Add(fromOffset);
                to = from.Add(Period);

                var reminders = await dbContext
                    .Reminders
                    .AsNoTracking()
                    .Where(e => e.RemindAt >= from && e.RemindAt <= to)
                    .ToListAsync(stoppingToken);
                
                var scheduledTasks = tasks.Select(e => new ScheduledAction
                {
                    Action = () => trayIcon.ShowBalloonTip(e.Title, e.Description, BalloonIcon.Info),
                    Delay = e.RemindAt - DateTimeOffset.UtcNow,
                    ActionId = e.Id,
                });
                
                var scheduledReminders = reminders.Select(e => new ScheduledAction
                {
                    Action = () => trayIcon.ShowBalloonTip("Напоминание", e.Text, BalloonIcon.Info),
                    Delay = e.RemindAt - DateTimeOffset.UtcNow,
                    ActionId = e.Id,
                });

                Scheduler.Schedule(scheduledTasks.Union(scheduledReminders));
            }
            catch (Exception ex)
            {
                trayIcon.ShowBalloonTip("Ошибка", ex.Message, BalloonIcon.Error);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested);
    }
}