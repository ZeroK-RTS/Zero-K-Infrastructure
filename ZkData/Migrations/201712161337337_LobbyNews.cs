namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LobbyNews : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LobbyNews",
                c => new
                    {
                        LobbyNewsID = c.Int(nullable: false, identity: true),
                        EventTime = c.DateTime(),
                        Created = c.DateTime(nullable: false),
                        Title = c.String(),
                        Text = c.String(),
                        Url = c.String(),
                        ImageExtension = c.String(maxLength: 50),
                        AuthorAccountID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.LobbyNewsID)
                .ForeignKey("dbo.Accounts", t => t.AuthorAccountID, cascadeDelete: true)
                .Index(t => t.AuthorAccountID);
            
            DropColumn("dbo.News", "LobbyPlaintext");
        }
        
        public override void Down()
        {
            AddColumn("dbo.News", "LobbyPlaintext", c => c.String());
            DropForeignKey("dbo.LobbyNews", "AuthorAccountID", "dbo.Accounts");
            DropIndex("dbo.LobbyNews", new[] { "AuthorAccountID" });
            DropTable("dbo.LobbyNews");
        }
    }
}
