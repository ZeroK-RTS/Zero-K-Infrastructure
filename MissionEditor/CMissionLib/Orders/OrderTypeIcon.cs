namespace CMissionLib
{
	public abstract class OrderTypeIcon : IOrder
	{
		public abstract string OrderType { get; }
		public abstract string Name { get; }
		public LuaTable GetLuaMap(Mission mission)
		{
			return LuaTable.Empty;
		}
	}
}