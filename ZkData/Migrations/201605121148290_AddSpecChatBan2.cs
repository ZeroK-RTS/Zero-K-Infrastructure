namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSpecChatBan2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Punishments", "BanSpecChat", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Punishments", "BanSpecChat");
        }
    }
}
