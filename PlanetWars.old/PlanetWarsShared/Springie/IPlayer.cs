namespace PlanetWarsShared.Springie
{
	public interface IPlayer
	{
		string Name { get; }
		bool IsCommanderInChief { get; }
		string FactionName { get; }
		int RankOrder { get; }
		string RankText { get; }
		ReminderEvent ReminderEvent { get; set; }
		ReminderLevel ReminderLevel { get; set; }
		ReminderRoundInitiative ReminderRoundInitiative { get; set; }
	}
}