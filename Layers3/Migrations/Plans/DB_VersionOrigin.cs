using Homura.ORM.Mapping;
using Homura.ORM.Migration;
using Homura.ORM.Setup;

namespace Layers3.Migrations.Plans
{
    internal class DB_VersionOrigin : ChangePlan<VersionOrigin>
    {
        public DB_VersionOrigin(VersioningMode mode) : base(mode)
        {
        }

        public override IEnumerable<IEntityVersionChangePlan> VersionChangePlanList
        {
            get
            {
                yield return new Drive_VersionOrigin(Mode);
            }
        }
    }
}
