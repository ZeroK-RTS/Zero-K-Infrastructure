namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SpringFilesUnitsyncAttempts : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SpringFilesUnitsyncAttempts",
                c => new
                    {
                        SpringFilesUnitsyncAttemptID = c.Int(nullable: false, identity: true),
                        FileName = c.String(),
                        Time = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.SpringFilesUnitsyncAttemptID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.SpringFilesUnitsyncAttempts");
        }
    }
}
