namespace ZkData.Migrations
{
    using System.Data.Entity.Migrations;

    /// <summary>
    /// Moves the CampaignEvents foreign key onto the column it was always meant to be on.
    ///
    /// In January 2015, FixEventPlanetCampaignRelation swapped the CampaignID and PlanetID
    /// columns by renaming them, and recreated the indexes - but it did not touch the
    /// foreign keys. The key created on the column originally named CampaignID stayed bound
    /// to that physical column, which is now called PlanetID. So the database has
    ///
    ///     FOREIGN KEY (PlanetID) -> Campaigns
    ///
    /// where every sibling table - CampaignLinks, CampaignJournals,
    /// AccountCampaignProgresses - has FOREIGN KEY (CampaignID) -> Campaigns. For eleven
    /// years CampaignEvents.PlanetID has been constrained against Campaigns.CampaignID,
    /// while the actual campaign reference has had no constraint at all.
    ///
    /// Found by building the same schema twice - once from the EF6 migrations and once
    /// from an EF Core model - and diffing them. See ZkData/EFCORE-MIGRATION.md.
    ///
    /// BEFORE APPLYING THIS TO A DATABASE WITH DATA, check both directions:
    ///
    ///     -- rows the new constraint would reject
    ///     SELECT COUNT(*) FROM dbo.CampaignEvents e
    ///     WHERE NOT EXISTS (SELECT 1 FROM dbo.Campaigns c WHERE c.CampaignID = e.CampaignID);
    ///
    ///     -- rows only the old constraint was holding up
    ///     SELECT COUNT(*) FROM dbo.CampaignEvents e
    ///     WHERE e.PlanetID IS NOT NULL
    ///       AND NOT EXISTS (SELECT 1 FROM dbo.Campaigns c WHERE c.CampaignID = e.PlanetID);
    ///
    /// The first must be zero or this migration fails. The second should be zero too; if it
    /// is not, the old constraint has been rejecting legitimate planet references.
    /// </summary>
    public partial class FixCampaignEventsCampaignForeignKey : DbMigration
    {
        /// <summary>
        /// The constraint has to be dropped by name, not by column. Its name still says
        /// CampaignID - and still says CampaignEvent and Campaign in the singular, from
        /// before the tables were pluralised - while the column underneath it is PlanetID.
        /// That mismatch is the whole defect, and it means DropForeignKey(table, column,
        /// principal) looks for a constraint that does not exist.
        /// </summary>
        private const string MisdirectedKey = "FK_dbo.CampaignEvent_dbo.Campaign_CampaignID";

        public override void Up()
        {
            DropForeignKey("dbo.CampaignEvents", MisdirectedKey);
            AddForeignKey("dbo.CampaignEvents", "CampaignID", "dbo.Campaigns", "CampaignID");
        }

        public override void Down()
        {
            DropForeignKey("dbo.CampaignEvents", "CampaignID", "dbo.Campaigns");
            Sql("ALTER TABLE [dbo].[CampaignEvents] ADD CONSTRAINT [" + MisdirectedKey +
                "] FOREIGN KEY ([PlanetID]) REFERENCES [dbo].[Campaigns] ([CampaignID])");
        }
    }
}
