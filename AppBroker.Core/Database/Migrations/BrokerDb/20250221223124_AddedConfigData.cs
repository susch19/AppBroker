﻿using Microsoft.EntityFrameworkCore.Migrations;

using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using static AppBroker.Core.Database.Migrations.BrokerDb.Initial;

using DayOfWeek = AppBroker.Core.Models.DayOfWeek;

namespace AppBroker.Core.Database.Migrations.BrokerDb
{
    public partial class AddedConfigData : Migration, IAutoMigrationTypeProvider
    {
        public IReadOnlyList<Type> GetEntityTypes() => new Type[]
        {
            typeof(DeviceModel),
            typeof(HeaterConfigModel),
            typeof(DeviceMappingModel),
            typeof(ConfigDataModel)
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.SetUpgradeOperations(this);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.SetDowngradeOperations(this);
        }

        [Table("Devices")]
        public class DeviceModel
        {
            [Key]
            public long Id { get; set; }
            [MaxLength(200)]
            public string TypeName { get; set; } = "";
            [MaxLength(200)]
            public string? FriendlyName { get; set; }

            public string? LastState { get; set; }
            public DateTime? LastStateChange { get; set; }
            public bool StartAutomatically { get; set; }
            public string? DeserializationData { get; set; }
        }
        [Table("HeaterConfigs")]
        public class HeaterConfigModel
        {
            [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public long Id { get; set; }
            public long? DeviceId { get; set; }
            public DayOfWeek DayOfWeek { get; set; }
            public DateTime TimeOfDay { get; set; }
            public double Temperature { get; set; }

            [ForeignKey(nameof(DeviceId))]
            public virtual DeviceModel? Device { get; set; }
        }

        [Table("DeviceToDeviceMappings")]
        public class DeviceMappingModel
        {
            [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public long Id { get; set; }

            [ForeignKey("ParentId")]
            public virtual DeviceModel? Parent { get; set; }
            [ForeignKey("ChildId")]
            public virtual DeviceModel? Child { get; set; }
        }
        [Table("ConfigDatas")]
        public class ConfigDataModel
        {
            [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
            public string Key { get; set; }

            public string Value { get; set; }
        }

    }
}
