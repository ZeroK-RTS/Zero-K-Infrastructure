using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Saves the current camera state (position, direction, mode)
	/// Can be loaded with <see cref="RestoreCameraStateAction"/>
	/// </summary>
	[DataContract]
	public class SaveCameraStateAction : Action
	{

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Save Camera State";
		}
	}
}