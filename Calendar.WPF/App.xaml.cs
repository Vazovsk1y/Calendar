using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Calendar.DAL.PostgreSQL;
using Calendar.WPF.Infrastructure;
using Calendar.WPF.ViewModels;
using Calendar.WPF.Views;
using CommunityToolkit.Mvvm.Input;
using Hardcodet.Wpf.TaskbarNotification;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Calendar.WPF;

public partial class App : Application
{
    private static readonly IHost Host;
    
    public static IServiceProvider Services { get; }
    
    static App()
    {
        var host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureServices((ctx, e) =>
            {
                var trayIcon = new TaskbarIcon
                {
                    Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Calendar.WPF.icon.ico")!),
                };
                
                var menu = new ContextMenu
                {
                    Items =
                    {
                        new MenuItem
                        {
                            Header = "Закрыть",
                            Icon = new PackIcon { Kind = PackIconKind.Shutdown, },
                            Command = new RelayCommand(() =>
                            {
                                Current?.Shutdown();
                                trayIcon.Visibility = Visibility.Collapsed;
                            }),
                        },
                    },
                };
                
                trayIcon.ContextMenu = menu;
                
                e.AddSingleton(trayIcon);
                
                e.AddSingleton<MainWindow>();
                e.AddSingleton<MainWindowViewModel>();
                
                e.AddDbContext<CalendarDbContext>(o => o.UseNpgsql(ctx.Configuration.GetConnectionString("Database")));

                e.AddHostedService<NotificationsSchedulerService>();

                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    var snackbarMessageQueue = new SnackbarMessageQueue { DiscardDuplicates = true };
                    e.AddSingleton<ISnackbarMessageQueue>(snackbarMessageQueue);
                });
            })
            .Build();
        
        Host = host;
        Services = Host.Services;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
        dbContext.Database.Migrate();
        scope.Dispose();
        
        var mainWindow = Services.GetRequiredService<MainWindow>();
        var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainWindowViewModel;
        mainWindow.Closing += OnMainWindowClosing;
        mainWindow.Show();
        
        var trayIcon = Services.GetRequiredService<TaskbarIcon>();
        trayIcon.Visibility = Visibility.Visible;
        trayIcon.TrayMouseDoubleClick += OnTrayMouseDoubleClick;
        Host.Start();
    }

    private static void OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        e.Handled = true;
    }

    private static void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        var mainWindow = sender as MainWindow ?? Services.GetRequiredService<MainWindow>();
        mainWindow.Hide();
    }
}