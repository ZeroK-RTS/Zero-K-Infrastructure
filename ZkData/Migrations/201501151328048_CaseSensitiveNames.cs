namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CaseSensitiveNames : DbMigration
    {
        public override void Up()
        {
            DropIndex("Accounts","IX_Name");
            Sql("ALTER TABLE Accounts ALTER COLUMN Name VARCHAR(2000) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL");
            CreateIndex("Accounts", "Name", true,"IX_Name");
        }
        
        public override void Down()
        {
        }
    }
}
