namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveEngineBranchAutoUpdate : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.AutohostConfigs", "AutoUpdateSpringBranch");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AutohostConfigs", "AutoUpdateSpringBranch", c => c.String(maxLength: 50));
        }
    }
}
