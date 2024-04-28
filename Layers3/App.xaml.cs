using DokanNet;
using DokanNet.Logging;
using Homura.ORM;
using Homura.ORM.Setup;
using Layers3.Migrations.Plans;
using Layers3.Views;
using Prism.Ioc;
using Prism.Unity;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Resources;

namespace Layers3
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        private NotifyIcon _notifyIcon;
        private Dokan _dokan;
        private ConsoleLogger _loggerDokan;

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.MainWindow, ViewModels.MainWindowViewModel>();
            containerRegistry.RegisterForNavigation<Views.RegisterNew, ViewModels.RegisterNewViewModel>();
            containerRegistry.RegisterForNavigation<Views.ShowRecommendedPolicy, ViewModels.ShowRecommendedPolicyViewModel>();
        }

        protected override Window CreateShell()
        {
            var window = Container.Resolve<MainWindow>();
            return window;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var dir = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/dhq_boiler/Layers3";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            ConnectionManager.SetDefaultConnection(Guid.Parse("7FCE6B45-DBC0-4772-A688-F8AB38D2CEC6"), $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/dhq_boiler/Layers3/layers3.db", typeof(SQLiteConnection));
            
            try
            {
                var dataVersionManager = new DataVersionManager
                {
                    CurrentConnection = ConnectionManager.DefaultConnection
                };
                dataVersionManager.RegisterChangePlan(new DB_VersionOrigin(VersioningMode.ByTick));
                dataVersionManager.SetDefault();
                dataVersionManager.UpgradeToTargetVersion();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

            base.OnStartup(e);

            MainWindow.Visibility = Visibility.Hidden;

            _notifyIcon = new NotifyIcon
            {
                Icon = GetIconFromResource("dhq_boiler_icon.ico"),
                Visible = true,
                Text = "Layers3",
                ContextMenuStrip = new ContextMenuStrip()
            };

            // コンテキストメニューを設定
            _notifyIcon.ContextMenuStrip.Items.Add("Layers3", null, (sender, eventArgs) => MainWindow.Visibility = Visibility.Visible);
            _notifyIcon.ContextMenuStrip.Items.Add("終了", null, Exit_Click);

            var vm = MainWindow.DataContext as ViewModels.MainWindowViewModel;

            try
            {
                _loggerDokan = new ConsoleLogger("[Dokan]");
                vm.Dokan = _dokan = new Dokan(_loggerDokan);

                if (vm.Drives.Any())
                {
                    foreach (var drive in vm.Drives)
                    {
                        drive.Dokan = _dokan;
                        drive.Mount();
                    }
                }
            }
            catch (Exception ex) 
            {
                
            }
        }
        private void Exit_Click(object sender, EventArgs e)
        {
            // 終了メニューがクリックされたらアプリケーションを終了
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _dokan.Dispose();
                _loggerDokan.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

            base.OnExit(e);
        }

        private static Icon GetIconFromResource(string resourceName)
        {
            // リソースファイルからアイコンを取得
            Uri uri = new Uri($"pack://application:,,,/Resources/{resourceName}");
            StreamResourceInfo info = System.Windows.Application.GetResourceStream(uri);

            // System.Drawing.Iconに変換
            using (Stream iconStream = info.Stream)
            {
                return new Icon(iconStream);
            }
        }
    }

}
