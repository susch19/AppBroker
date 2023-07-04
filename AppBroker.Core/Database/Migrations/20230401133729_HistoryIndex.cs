using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace AppBroker.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class HistoryIndex : Migration, IAutoMigrationTypeProvider
    {


        public IReadOnlyList<Type> GetEntityTypes()
        {
            return new[]
            {
                typeof(HistoryDevice),
                typeof(HistoryProperty),
                typeof(HistoryValueBase),
                typeof(HistoryValueBool),
                typeof(HistoryValueDateTime),
                typeof(HistoryValueDouble),
                typeof(HistoryValueLong),
                typeof(HistoryValueString),
                typeof(HistoryValueTimeSpan),
                typeof(HistoryValueHeaterConfig),
            };
        }
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.SetUpgradeOperations(this);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.SetDowngradeOperations(this);
        }
        [Table("HistoryDevices")]
        public class HistoryDevice
        {

            [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public long DeviceId { get; set; }
        }

        [Table("HistoryProperties")]
        public class HistoryProperty
        {
            [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public long Id { get; set; }

            public bool Enabled { get; set; }
            [StringLength(200)]
            public string PropertyName { get; set; }


            public virtual HistoryDevice Device { get; set; }

        }

        [Table("HistoryValueBases"),
    Index(nameof(Timestamp), nameof(HistoryValueId), IsUnique = true, Name = "HistoryValueTimestampIndex")]
        public class HistoryValueBase
        {
            [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public long Id { get; set; }
            public DateTime Timestamp { get; set; }
            public long HistoryValueId { get; set; }

            //TODO Add retentionpolicy
            //TODO Add past concation? (Example after 1 Month group values for each 10 Minutes into one, for devices which have a lot of state changes)

            [ForeignKey(nameof(HistoryValueId))]
            public virtual HistoryProperty HistoryValue { get; set; }
        }

        [Table("HistoryValueHeaterConfigs")]
        public class HistoryValueHeaterConfig : HistoryValueBase
        {
            public DayOfWeek DayOfWeek { get; set; }
            public DateTime TimeOfDay { get; set; }
            public double Temperature { get; set; }

            public HistoryValueHeaterConfig()
            {
            }
        }
        [Table("HistoryValueBools")]
        public class HistoryValueBool : HistoryValueBase
        {
            public bool Value { get; set; }

            public HistoryValueBool()
            {
            }
            public HistoryValueBool(bool value)
            {
                Value = value;
            }
        }
        [Table("HistoryValueDateTimes")]
        public class HistoryValueDateTime : HistoryValueBase
        {
            public DateTime Value { get; set; }

            public HistoryValueDateTime()
            {
            }
            public HistoryValueDateTime(DateTime value)
            {
                Value = value;
            }
        }
        [Table("HistoryValueDoubles")]
        public class HistoryValueDouble : HistoryValueBase
        {
            public double Value { get; set; }

            public HistoryValueDouble()
            {
            }
            public HistoryValueDouble(double value)
            {
                Value = value;
            }
        }
        [Table("HistoryValueLongs")]
        public class HistoryValueLong : HistoryValueBase
        {
            public long Value { get; set; }

            public HistoryValueLong()
            {
            }
            public HistoryValueLong(long value)
            {
                Value = value;
            }
        }
        [Table("HistoryValueStrings")]
        public class HistoryValueString : HistoryValueBase
        {
            public string Value { get; set; }

            public HistoryValueString()
            {
            }
            public HistoryValueString(string value)
            {
                Value = value;
            }
        }
        [Table("HistoryValueTimeSpans")]
        public class HistoryValueTimeSpan : HistoryValueBase
        {
            public TimeSpan Value { get; set; }

            public HistoryValueTimeSpan()
            {
            }
            public HistoryValueTimeSpan(TimeSpan value)
            {
                Value = value;
            }
        }
    }
}
