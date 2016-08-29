namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRapidTags : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Resources", "RapidTag", c => c.String(maxLength: 100));
            CreateIndex("dbo.Resources", "RapidTag");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Resources", new[] { "RapidTag" });
            DropColumn("dbo.Resources", "RapidTag");
        }
    }
}
