namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ModactionMessageOnly : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Punishments", "MessageOnly", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Punishments", "MessageOnly");
        }
    }
}
