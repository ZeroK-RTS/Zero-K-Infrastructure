namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OptionalHostIDAndMode : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.SpringBattles", new[] { "HostAccountID" });
            AddColumn("dbo.SpringBattles", "Mode", c => c.Int(nullable: false));
            AlterColumn("dbo.SpringBattles", "HostAccountID", c => c.Int());
            CreateIndex("dbo.SpringBattles", "HostAccountID");
        }
        
        public override void Down()
        {
            DropIndex("dbo.SpringBattles", new[] { "HostAccountID" });
            AlterColumn("dbo.SpringBattles", "HostAccountID", c => c.Int(nullable: false));
            DropColumn("dbo.SpringBattles", "Mode");
            CreateIndex("dbo.SpringBattles", "HostAccountID");
        }
    }
}
