using Amazon.Runtime;
using Amazon.S3;
using Homura.ORM;
using Layers3.Helpers;
using Layers3.Models;
using Layers3.ViewModels;
using System.Data;
using PasswordVault = Windows.Security.Credentials.PasswordVault;

namespace Layers3.Daos
{
    internal class DriveDao : Dao<Drive>
    {
        public DriveDao() : base()
        { }

        public DriveDao(Type entityVersionType) : base(entityVersionType)
        { }

        protected override Drive ToEntity(IDataRecord reader, params IColumn[] columns)
        {
            var entry = new Drive();
            entry.Id.Value = CatchThrow(() => GetColumnValue(reader, Columns.Single(c => c.ColumnName == nameof(entry.Id)), Table).CastStruct<Guid>());
            entry.Region.Value = CatchThrow(() => GetColumnValue(reader, Columns.Single(c => c.ColumnName == nameof(entry.Region)), Table).CastClass<string>());
            entry.BucketName.Value = CatchThrow(() => GetColumnValue(reader, Columns.Single(c => c.ColumnName == nameof(entry.BucketName)), Table).CastClass<string>());
            entry.DriveLetter.Value = CatchThrow(() => GetColumnValue(reader, Columns.Single(c => c.ColumnName == nameof(entry.DriveLetter)), Table).CastClass<string>());

            var vm = App.Current.MainWindow.DataContext as MainWindowViewModel;
            entry.Dokan = vm.Dokan;
            var myVault = new PasswordVault();
            var credential1 = myVault.Retrieve($"{entry.RegionEndpoint.Value.DisplayName}/{entry.BucketName.Value}/apikey", Environment.UserName);
            var credential2 = myVault.Retrieve($"{entry.RegionEndpoint.Value.DisplayName}/{entry.BucketName.Value}/secret", Environment.UserName);

            entry.Client = new AmazonS3Client(new BasicAWSCredentials(credential1.Password, credential2.Password), entry.RegionEndpoint.Value);

            return entry;
        }
    }
}
