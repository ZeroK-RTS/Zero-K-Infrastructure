using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Restores the camera to the state stored with <see cref="SaveCameraStateAction"/>
	/// </summary>
	[DataContract]
	public class RestoreCameraStateAction : Action
	{

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Restore Camera State";
		}
	}
}