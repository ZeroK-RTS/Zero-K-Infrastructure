using System.Runtime.Serialization;

namespace CMissionLib
{
	public interface IOrder
	{
		[DataMember]
		string OrderType { get; }

		string Name { get; }

		LuaTable GetLuaMap(Mission mission);
	}
}