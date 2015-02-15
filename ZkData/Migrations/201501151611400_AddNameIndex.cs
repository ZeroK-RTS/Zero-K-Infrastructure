namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNameIndex : DbMigration
    {
        public override void Up()
        {
            CreateIndex("Accounts", "Name", true, "IX_Name");
        }
        
        public override void Down()
        {
        }
    }
}
