namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NewsLobbyPlainText : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.News", "LobbyPlaintext", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.News", "LobbyPlaintext");
        }
    }
}
