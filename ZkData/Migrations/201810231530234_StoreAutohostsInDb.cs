namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StoreAutohostsInDb : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Autohosts",
                c => new
                    {
                        AutohostID = c.Int(nullable: false, identity: true),
                        MinimumMapSupportLevel = c.Int(nullable: false),
                        AutohostMode = c.Int(nullable: false),
                        InviteMMPlayers = c.Int(nullable: false),
                        MaxElo = c.Int(nullable: false),
                        MinElo = c.Int(nullable: false),
                        MaxLevel = c.Int(nullable: false),
                        MinLevel = c.Int(nullable: false),
                        MaxRank = c.Int(nullable: false),
                        MinRank = c.Int(nullable: false),
                        MaxPlayers = c.Int(nullable: false),
                        Title = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.AutohostID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Autohosts");
        }
    }
}
