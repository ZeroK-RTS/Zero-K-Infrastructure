namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RushActivationTime : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StructureTypes", "RushActivationTime", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.StructureTypes", "RushActivationTime");
        }
    }
}
