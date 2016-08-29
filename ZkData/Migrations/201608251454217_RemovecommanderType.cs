namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovecommanderType : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.SpringBattlePlayers", "CommanderType");
        }
        
        public override void Down()
        {
            AddColumn("dbo.SpringBattlePlayers", "CommanderType", c => c.String(maxLength: 50));
        }
    }
}
