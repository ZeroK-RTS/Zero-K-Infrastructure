namespace ModStats
{
	partial class ModStatsLinqDataContext
	{
		#region Other methods

		partial void OnCreated()
		{
			CommandTimeout = 120;
		}

		#endregion
	}
}