using System.Timers;
using Timer = System.Timers.Timer;

namespace Calendar.WPF.Infrastructure;

public static class Scheduler
{
    private static readonly Dictionary<Guid, (Timer timer, ElapsedEventHandler handler)> JobIdToAssociatedTimerWithHandler = new();
    private static readonly Lock Locker = new();

    public static void Schedule(params IEnumerable<ScheduledAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        
        using var lockScope = Locker.EnterScope();
        foreach (var action in actions)
        {
            ScheduleCore(action.Action, action.Delay, action.ActionId);
        }
    }

    public static void Schedule(Action action, TimeSpan delay, Guid actionId)
    {
        ArgumentNullException.ThrowIfNull(action);
        
        using var lockScope = Locker.EnterScope();
        ScheduleCore(action, delay, actionId);
    }

    public static bool Cancel(Guid actionId)
    {
        using var lockScope = Locker.EnterScope();
        
        if (!JobIdToAssociatedTimerWithHandler.Remove(actionId, out var item))
        {
            return false;
        }
        
        item.timer.Elapsed -= item.handler;
        item.timer.Dispose();
        return true;
    }
    
    private static void ScheduleCore(Action action, TimeSpan delay, Guid actionId)
    {
        if (delay == TimeSpan.Zero)
        {
            action();
            return;
        }
        
        Timer timer;
        if (JobIdToAssociatedTimerWithHandler.Remove(actionId, out var item))
        {
            timer = item.timer;
            timer.Elapsed -= item.handler;
            timer.Dispose();
        }
        
        timer = new Timer(delay) { AutoReset = false };
        var handler = new ElapsedEventHandler(OnTimerElapsed);
        
        timer.Elapsed += handler;
        JobIdToAssociatedTimerWithHandler[actionId] = (timer, handler);
        
        timer.Start();
        return;
        
        void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                action();
            }
            finally
            {
                using var lockScope = Locker.EnterScope();
                if (JobIdToAssociatedTimerWithHandler.Remove(actionId, out var i))
                {
                    i.timer.Elapsed -= i.handler;
                    i.timer.Dispose();
                }
            }
        }
    }
}

public class ScheduledAction
{
    public required Action Action { get; init; } 
    
    public required TimeSpan Delay { get; init; }
    
    public required Guid ActionId { get; init; }
}