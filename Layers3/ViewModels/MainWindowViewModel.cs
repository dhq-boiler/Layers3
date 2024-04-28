using Amazon.S3;
using DokanNet;
using Layers3.Models;
using Layers3.Views;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Prism.Unity;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Layers3.Daos;
using ObservableExtensions = Reactive.Bindings.TinyLinq.ObservableExtensions;

namespace Layers3.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        private readonly CompositeDisposable _disposables = new();
        public ReactiveCollection<Drive> Drives { get; } = new();
        public ReactiveCollection<Drive> SelectedDrives { get; set; } = new();
        public ReactiveCommandSlim<CancelEventArgs> ClosingCommand { get; }
        public ReactiveCommandSlim<RoutedEventArgs> ClosedCommand { get; }
        public ReactiveCommandSlim RegisterNewCommand { get; }
        public ReactiveCommandSlim UnregisterCommand { get; }
        public ReactiveCommandSlim<Drive> ShowRecommendedPolicyCommand { get; }
        public ReactiveCommandSlim<SelectionChangedEventArgs> SelectedItemsCommand { get; }
        public ReactiveCommandSlim LoadedCommand { get; }
        public Dokan Dokan { get; internal set; }

        public MainWindowViewModel()
        {
            ClosingCommand = new ReactiveCommandSlim<CancelEventArgs>().WithSubscribe(eventArgs =>
            {
                if (App.Current.MainWindow is null)
                    return;
                App.Current.MainWindow.Visibility = Visibility.Hidden;
            }).AddTo(_disposables);
            ClosedCommand = new ReactiveCommandSlim<RoutedEventArgs>().WithSubscribe(eventArgs =>
            {
                eventArgs.Handled = true;
            }).AddTo(_disposables);
            RegisterNewCommand = new ReactiveCommandSlim().WithSubscribe(() =>
            {
                var vm = App.Current.MainWindow.DataContext as MainWindowViewModel;
                var dialogService =
                    (App.Current as PrismApplication).Container.Resolve(typeof(IDialogService)) as IDialogService;
                dialogService.ShowDialog(nameof(RegisterNew), null, async result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        var dao = new DriveDao();
                        var region = result.Parameters.GetValue<string>("region");
                        var bucketName = result.Parameters.GetValue<string>("bucketName");
                        var driveLetter = result.Parameters.GetValue<string>("mountPoint");
                        var client = result.Parameters.GetValue<AmazonS3Client>("client");
                        var newDrive = new Drive(Dokan, client)
                        {
                            Region =
                            {
                                Value = region
                            },
                            BucketName =
                            {
                                Value = bucketName
                            },
                            DriveLetter =
                            {
                                Value = driveLetter
                            }
                        };
                        await dao.InsertAsync(newDrive);
                        vm.Drives.Add(newDrive);
                        ShowRecommendedPolicyCommand.Execute(newDrive);
                    }
                });
            }).AddTo(_disposables);
            UnregisterCommand = ObservableExtensions.Select(SelectedDrives.ObserveProperty(x => x.Count), x => x > 0).ToReactiveCommandSlim().WithSubscribe(() =>
            {
                var dao = new DriveDao();
                var targets = SelectedDrives.ToList();
                targets.ForEach(async target =>
                {
                    target.Unmount();
                    Drives.Remove(target);
                    await dao.DeleteWhereIDIsAsync(target.Id.Value);
                });
            }).AddTo(_disposables);
            SelectedItemsCommand = new ReactiveCommandSlim<SelectionChangedEventArgs>()
                .WithSubscribe(ea =>
                {
                    var listView = (System.Windows.Controls.ListView)ea.Source;
                    SelectedDrives.Clear();
                    SelectedDrives.AddRange(listView.SelectedItems.Cast<Drive>().ToList());
                }).AddTo(_disposables);
            ShowRecommendedPolicyCommand = new ReactiveCommandSlim<Drive>().WithSubscribe(drive =>
            {
                var dialogService =
                    (App.Current as PrismApplication).Container.Resolve(typeof(IDialogService)) as IDialogService;
                var dialogParameters = new DialogParameters();
                dialogParameters.Add("bucketName", drive.BucketName.Value);
                dialogService.ShowDialog(nameof(ShowRecommendedPolicy), dialogParameters, result => { });
            }).AddTo(_disposables);
            LoadedCommand = new ReactiveCommandSlim().WithSubscribe(() =>
            {
                var dao = new DriveDao();
                var drives = dao.FindAll();
                Drives.AddRange(drives);
            }).AddTo(_disposables);
        }
    }
}
