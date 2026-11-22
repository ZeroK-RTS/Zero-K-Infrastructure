using Microsoft.EntityFrameworkCore;

namespace ZkData
{
    /// <summary>
    /// Table names where EF Core's convention disagrees with the database.
    ///
    /// EF6 named a table after the pluralised entity type; EF Core names it after the DbSet
    /// property. For most entities those coincide, and for four they do not - two DbSets
    /// are singular, and two are named for what they are used for rather than what they
    /// hold.
    ///
    /// Written out rather than fixed with a pluralisation convention on purpose. A
    /// convention has to be right about every entity, including <c>News</c>, whose table is
    /// <c>News</c> and which no naive pluraliser handles. Four names taken from
    /// db/schema/schema.txt are four facts; a rule would be a guess with 89 chances to be
    /// wrong.
    /// </summary>
    public partial class ZkDataContext
    {
        partial void ConfigureTableNames(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountCampaignProgress>().ToTable("AccountCampaignProgresses");
            modelBuilder.Entity<AccountCampaignJournalProgress>().ToTable("AccountCampaignJournalProgresses");

            // DbSet<Word> is called IndexWords, and DbSet<ForumPostWord> IndexForumPosts,
            // after the forum search index they serve.
            modelBuilder.Entity<Word>().ToTable("Words");
            modelBuilder.Entity<ForumPostWord>().ToTable("ForumPostWords");
        }
    }
}
