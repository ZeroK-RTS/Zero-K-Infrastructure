namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveSpringieLevel : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Accounts", "SpringieLevel");
            DropColumn("dbo.Punishments", "SetRightsToZero");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Punishments", "SetRightsToZero", c => c.Boolean(nullable: false));
            AddColumn("dbo.Accounts", "SpringieLevel", c => c.Int(nullable: false));
        }
    }
}
