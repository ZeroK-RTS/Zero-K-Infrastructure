namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLobbyChannelTopics : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LobbyChannelTopics",
                c => new
                    {
                        ChannelName = c.String(nullable: false, maxLength: 200),
                        Topic = c.String(),
                        Author = c.String(),
                        SetDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ChannelName);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.LobbyChannelTopics");
        }
    }
}
