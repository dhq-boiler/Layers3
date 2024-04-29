using Amazon;
using Amazon.S3;
using DokanNet;
using DokanNet.Logging;
using Homura.ORM;
using Homura.ORM.Mapping;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.TinyLinq;
using System.Reactive.Disposables;

namespace Layers3.Models
{
    [DefaultVersion(typeof(VersionOrigin))]
    internal class Drive : EntityBaseObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private ConsoleLogger _loggerDokan;
        private DokanInstanceBuilder _dokanInstanceBuilder;
        private DokanInstance _dokanInstance;

        [Column("Id", "NUMERIC", 0), PrimaryKey]
        public ReactivePropertySlim<Guid> Id { get; } = new();

        [Column("Region", "TEXT", 1), NotNull]
        public ReactivePropertySlim<string> Region { get; } = new();

        [Column("BucketName", "TEXT", 2), NotNull]
        public ReactivePropertySlim<string> BucketName { get; } = new();

        [Column("DriveLetter", "TEXT", 3), NotNull]
        public ReactivePropertySlim<string> DriveLetter { get; } = new();

        public AmazonS3Client Client { get; internal set; }
        public Dokan Dokan { get; internal set; }
        public ReactivePropertySlim<bool> IsRunning { get; } = new();
        public ReadOnlyReactivePropertySlim<System.Windows.Media.Brush> IsRunningBrush { get; }
        public ReadOnlyReactivePropertySlim<RegionEndpoint> RegionEndpoint { get; }
        public ReactiveCommandSlim MountCommand { get; }
        public ReactiveCommandSlim UnmountCommand { get; }

        public Drive()
        {
            Id.Value = Guid.NewGuid();
            IsRunningBrush = IsRunning.Select(v => (System.Windows.Media.Brush)(v ? System.Windows.Media.Brushes.Lime : System.Windows.Media.Brushes.Red))
                .ToReadOnlyReactivePropertySlim();
            MountCommand = new ReactiveCommandSlim().WithSubscribe(Mount).AddTo(_disposables);
            UnmountCommand = IsRunning.ToReactiveCommandSlim().WithSubscribe(Unmount).AddTo(_disposables);
            RegionEndpoint = Region.Select(str =>
            {
                switch (str)
                {
                    case "us-east-1":
                        return Amazon.RegionEndpoint.USEast1;
                    case "us-east-2":
                        return Amazon.RegionEndpoint.USEast2;
                    case "us-west-1":
                        return Amazon.RegionEndpoint.USWest1;
                    case "us-west-2":
                        return Amazon.RegionEndpoint.USWest2;
                    case "af-south-1":
                        return Amazon.RegionEndpoint.AFSouth1;
                    case "ap-northeast-1":
                        return Amazon.RegionEndpoint.APNortheast1;
                    case "ap-northeast-2":
                        return Amazon.RegionEndpoint.APNortheast2;
                    case "ap-northeast-3":
                        return Amazon.RegionEndpoint.APNortheast3;
                    case "ap-east-1":
                        return Amazon.RegionEndpoint.APEast1;
                    case "ap-south-1":
                        return Amazon.RegionEndpoint.APSouth1;
                    case "ap-south-2":
                        return Amazon.RegionEndpoint.APSouth2;
                    case "ap-southeast-1":
                        return Amazon.RegionEndpoint.APSoutheast1;
                    case "ap-southeast-2":
                        return Amazon.RegionEndpoint.APSoutheast2;
                    case "ap-southeast-3":
                        return Amazon.RegionEndpoint.APSoutheast3;
                    case "ap-southeast-4":
                        return Amazon.RegionEndpoint.APSoutheast4;
                    case "ca-central-1":
                        return Amazon.RegionEndpoint.CACentral1;
                    case "ca-west-1":
                        return Amazon.RegionEndpoint.CAWest1;
                    case "eu-central-1":
                        return Amazon.RegionEndpoint.EUCentral1;
                    case "eu-central-2":
                        return Amazon.RegionEndpoint.EUCentral2;
                    case "eu-west-1":
                        return Amazon.RegionEndpoint.EUWest1;
                    case "eu-west-2":
                        return Amazon.RegionEndpoint.EUWest2;
                    case "eu-west-3":
                        return Amazon.RegionEndpoint.EUWest3;
                    case "eu-south-1":
                        return Amazon.RegionEndpoint.EUSouth1;
                    case "eu-south-2":
                        return Amazon.RegionEndpoint.EUSouth2;
                    case "eu-north-1":
                        return Amazon.RegionEndpoint.EUNorth1;
                    case "il-central-1":
                        return Amazon.RegionEndpoint.ILCentral1;
                    case "me-south-1":
                        return Amazon.RegionEndpoint.MECentral1;
                    case "me-central-1":
                        return Amazon.RegionEndpoint.MESouth1;
                    case "sa-east-1":
                        return Amazon.RegionEndpoint.SAEast1;
                    default:
                        return null;
                }
            }).ToReadOnlyReactivePropertySlim();
        }

        public Drive(Dokan? dokan, AmazonS3Client client) : this()
        {
            Client = client;
            Dokan = dokan;
        }

        public void Mount()
        {
            _loggerDokan = new ConsoleLogger("[Dokan]");
            _dokanInstanceBuilder = new DokanInstanceBuilder(Dokan)
                .ConfigureLogger(() => _loggerDokan)
                .ConfigureOptions(options =>
                {
                    options.Options = DokanOptions.DebugMode | DokanOptions.EnableNotificationAPI;
                    //options.SectorSize = 5 * 1024 * 1024;
                    options.MountPoint = $"{DriveLetter}:\\";
                });
            _dokanInstance = _dokanInstanceBuilder.Build(new S3FileSystem(Client, BucketName.Value, DriveLetter.Value));
            IsRunning.Value = true;
        }

        public void Unmount()
        {
            if (Dokan is null || !IsRunning.Value)
                return;
            Dokan.Unmount(DriveLetter.Value.First());
            IsRunning.Value = false;
        }

        public void Dispose()
        {
            _loggerDokan.Dispose();
            _dokanInstance.Dispose();
            Client.Dispose();
        }
    }
}
