using System.Data.Entity.Migrations;

namespace ElectronicsShop.Migrations
{
    public partial class AddRecipientColumnsToShipment : DbMigration
    {
        public override void Up()
        {
            // Add the new columns to the existing table
            //AddColumn("dbo.Shipments", "recipient_first_name", c => c.String(nullable: false, maxLength: 50));
            //AddColumn("dbo.Shipments", "recipient_last_name", c => c.String(nullable: false, maxLength: 50));
            //AddColumn("dbo.Shipments", "recipient_phone", c => c.String(maxLength: 20));
        }

        public override void Down()
        {
            // Drop the columns if rolling back
            DropColumn("dbo.Shipments", "recipient_phone");
            DropColumn("dbo.Shipments", "recipient_last_name");
            DropColumn("dbo.Shipments", "recipient_first_name");
        }
    }
}
