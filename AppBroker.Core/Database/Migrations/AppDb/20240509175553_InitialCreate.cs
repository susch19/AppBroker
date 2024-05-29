using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;

using Microsoft.EntityFrameworkCore.Migrations;

using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;


namespace AppBroker.Core.Database.AppDb.Migrations
{
    public partial class InitialCreate : Migration, IAutoMigrationTypeProvider
    {
        public IReadOnlyList<Type> GetEntityTypes() => new Type[]
        {
            typeof(AppModel),
            typeof(AppConfigModel)
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.SetUpgradeOperations(this);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.SetDowngradeOperations(this);
        }

        [Table("AppConfigs")]
        public class AppConfigModel
        {
            [Key, DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 0)]
            public string Key { get; set; }

            public string Value { get; set; }

            [ForeignKey("AppId"), Key, Column(Order = 1)]
            public AppModel App { get; set; }
        }
        [Table("Apps")]
        public class AppModel
        {
            [Key]
            public Guid Id { get; set; }
            public string Name { get; set; }

            [InverseProperty(nameof(AppConfigModel.App))]
            public ICollection<AppConfigModel> Configs { get; set; }
        }

    }
}
