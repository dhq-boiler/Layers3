using Homura.ORM;
using Homura.ORM.Mapping;
using Homura.ORM.Migration;
using Homura.ORM.Setup;
using Layers3.Daos;
using Layers3.Models;

namespace Layers3.Migrations.Plans
{
    internal class Drive_VersionOrigin : ChangePlan<Drive, VersionOrigin>
    {
        public Drive_VersionOrigin(VersioningMode mode) : base("Drive_0", PostMigrationVerification.TableExists, mode)
        {
        }

        public override async void CreateTable(IConnection connection)
        {
            var dao = new DriveDao(typeof(VersionOrigin))
            {
                CurrentConnection = connection
            };
            await dao.CreateTableIfNotExistsAsync();
            ++ModifiedCount;
            await dao.CreateIndexIfNotExistsAsync();
            ++ModifiedCount;
        }

        public override async void DropTable(IConnection connection)
        {
            var dao = new DriveDao(typeof(VersionOrigin))
            {
                CurrentConnection = connection
            };
            await dao.DropTableIfExistsAsync();
            ++ModifiedCount;
        }

        public override async void UpgradeToTargetVersion(IConnection connection)
        {
            CreateTable(connection);
        }
    }
}