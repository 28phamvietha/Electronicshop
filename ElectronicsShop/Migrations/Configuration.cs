using System.Data.Entity.Migrations;

namespace ElectronicsShop.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<ElectronicsShop.Models.Db_ElectronicsShop>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ElectronicsShop.Models.Db_ElectronicsShop context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}
