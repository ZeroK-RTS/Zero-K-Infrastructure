namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBanVotes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Punishments", "BanVotes", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Punishments", "BanVotes");
        }
    }
}
