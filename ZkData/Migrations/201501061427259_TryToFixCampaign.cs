namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TryToFixCampaign : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.CampaignJournals", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.AccountCampaignJournalProgresses", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.AccountCampaignProgresses", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.AccountCampaignVars", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignEvents", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignJournalVars", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignLinks", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignPlanets", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignPlanetVars", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignVars", "CampaignID", "dbo.Campaigns");
            DropPrimaryKey("dbo.Campaigns");
            AlterColumn("dbo.Campaigns", "CampaignID", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignJournals", "CampaignID", "dbo.Campaigns", "CampaignID", cascadeDelete: true);
            AddForeignKey("dbo.AccountCampaignJournalProgresses", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.AccountCampaignProgresses", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.AccountCampaignVars", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignEvents", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignJournalVars", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignLinks", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignPlanets", "CampaignID", "dbo.Campaigns", "CampaignID", cascadeDelete: true);
            AddForeignKey("dbo.CampaignPlanetVars", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignVars", "CampaignID", "dbo.Campaigns", "CampaignID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CampaignVars", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignPlanetVars", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignPlanets", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignLinks", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignJournalVars", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignEvents", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.AccountCampaignVars", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.AccountCampaignProgresses", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.AccountCampaignJournalProgresses", "CampaignID", "dbo.Campaigns");
            DropForeignKey("dbo.CampaignJournals", "CampaignID", "dbo.Campaigns");
            DropPrimaryKey("dbo.Campaigns");
            AlterColumn("dbo.Campaigns", "CampaignID", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignVars", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignPlanetVars", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignPlanets", "CampaignID", "dbo.Campaigns", "CampaignID", cascadeDelete: true);
            AddForeignKey("dbo.CampaignLinks", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignJournalVars", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignEvents", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.AccountCampaignVars", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.AccountCampaignProgresses", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.AccountCampaignJournalProgresses", "CampaignID", "dbo.Campaigns", "CampaignID");
            AddForeignKey("dbo.CampaignJournals", "CampaignID", "dbo.Campaigns", "CampaignID", cascadeDelete: true);
        }
    }
}
