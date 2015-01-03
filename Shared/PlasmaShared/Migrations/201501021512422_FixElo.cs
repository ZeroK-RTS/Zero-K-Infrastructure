namespace PlasmaShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixElo : DbMigration
    {
        public override void Up()
        {
            Sql("update Account set Elo = 1500, Elo1v1 = 1500, EloPw = 1500, EloWeight = 1, Elo1v1Weight = 1 where Elo < 300");
        }
        
        public override void Down()
        {
        }
    }
}
