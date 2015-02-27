#region using

using System;
using System.Net;

#endregion

namespace LobbyClient.Legacy
{
    [Obsolete]
	public enum SyncStatuses
	{
		Unknown = 0,
		Synced = 1,
		Unsynced = 2
	}
    [Obsolete]
	public class UserBattleStatus
	{
		public int AllyNumber;
		public bool IsReady;
		public bool IsSpectator;
		public DateTime JoinTime = DateTime.Now;
		public string Name;
		public string ScriptPassword;
		public int Side;
		public SyncStatuses SyncStatus = SyncStatuses.Unknown;
		public int TeamColor;
		public User LobbyUser;
		public int[] TeamColorRGB
		{
			get
			{
				var r = TeamColor & 255;
				var g = (TeamColor >> 8) & 255;
				var b = (TeamColor >> 16) & 255;
				return new[] { r, g, b };
			}
		}

		public int TeamNumber;
		public IPAddress ip = IPAddress.None;
		public int port;

		public UserBattleStatus() {}


		public UserBattleStatus(string name, User lobbyUser, string scriptPassword = null)
		{
			Name = name;
			ScriptPassword = scriptPassword;
			LobbyUser = lobbyUser;
		}

		public virtual UserBattleStatus Clone()
		{
			return (UserBattleStatus)MemberwiseClone();
		}

		public bool Equals(UserBattleStatus other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.AllyNumber == AllyNumber && Equals(other.ip, ip) && other.IsReady.Equals(IsReady) && other.IsSpectator.Equals(IsSpectator) &&
			       other.JoinTime.Equals(JoinTime) && Equals(other.Name, Name) && other.port == port && other.Side == Side &&
			       Equals(other.SyncStatus, SyncStatus) && other.TeamColor == TeamColor && other.TeamNumber == TeamNumber;
		}

		public int GetAllyChangedBattleStatus(int newAlly)
		{
			var status = 0;
			if (IsReady) status |= 2;
			status += (TeamNumber & 15) << 2;
			status += (newAlly & 15) << 6;
			if (!IsSpectator) status |= 1024;
			status += ((int)SyncStatus & 3) << 22;
			status += (Side & 15) << 24;
			return status;
		}

		public int GetReadyChangedBattleStatus(bool isReady)
		{
			var status = 0;
			if (isReady) status |= 2;
			status += (TeamNumber & 15) << 2;
			status += (AllyNumber & 15) << 6;
			if (!IsSpectator) status |= 1024;
			status += ((int)SyncStatus & 3) << 22;
			status += (Side & 15) << 24;
			return status;
		}

		public int GetSideChangedBattleStatus(int side)
		{
			var status = 0;
			if (IsReady) status |= 2;
			status += (TeamNumber & 15) << 2;
			status += (AllyNumber & 15) << 6;
			if (!IsSpectator) status |= 1024;
			status += ((int)SyncStatus & 3) << 22;
			status += (side & 15) << 24;
			return status;
		}

		public int GetSpectateChangedBattleStatus(bool spectate)
		{
			var status = 0;
			if (IsReady) status |= 2;
			status += (TeamNumber & 15) << 2;
			status += (AllyNumber & 15) << 6;
			if (!spectate) status |= 1024;
			status += ((int)SyncStatus & 3) << 22;
			status += (Side & 15) << 24;
			return status;
		}

		public int GetSyncChangedBattleStatus(SyncStatuses syncStatus)
		{
			var status = 0;
			if (IsReady) status |= 2;
			status += (TeamNumber & 15) << 2;
			status += (AllyNumber & 15) << 6;
			if (!IsSpectator) status |= 1024;
			status += ((int)syncStatus & 3) << 22;
			status += (Side & 15) << 24;
			return status;
		}

		public int GetTeamChangedBattleStatus(int newTeam)
		{
			var status = 0;
			if (IsReady) status |= 2;
			status += (newTeam & 15) << 2;
			status += (AllyNumber & 15) << 6;
			if (!IsSpectator) status |= 1024;
			status += ((int)SyncStatus & 3) << 22;
			status += (Side & 15) << 24;
			return status;
		}

		public void SetFrom(int status, int color, string name)
		{
			Name = name;
			SetFrom(status, color);
		}

		public void SetFrom(int status, int color)
		{
			IsReady = (status & 2) > 0;
			TeamNumber = (status >> 2) & 15;
			AllyNumber = (status >> 6) & 15;
			IsSpectator = (status & 1024) == 0;
			SyncStatus = (SyncStatuses)((status >> 22) & 3);
			Side = (status >> 24) & 15;
			TeamColor = color;
		}

		public int ToInt()
		{
			var status = 0;
			if (IsReady) status |= 2;
			status += (TeamNumber & 15) << 2;
			status += (AllyNumber & 15) << 6;
			if (!IsSpectator) status |= 1024;
			status += ((int)SyncStatus & 3) << 22;
			status += (Side & 15) << 24;
			return status;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof(UserBattleStatus)) return false;
			return Equals((UserBattleStatus)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = AllyNumber;
				result = (result*397) ^ (ip != null ? ip.GetHashCode() : 0);
				result = (result*397) ^ IsReady.GetHashCode();
				result = (result*397) ^ IsSpectator.GetHashCode();
				result = (result*397) ^ JoinTime.GetHashCode();
				result = (result*397) ^ (Name != null ? Name.GetHashCode() : 0);
				result = (result*397) ^ port;
				result = (result*397) ^ Side;
				result = (result*397) ^ SyncStatus.GetHashCode();
				result = (result*397) ^ TeamColor;
				result = (result*397) ^ TeamNumber;
				return result;
			}
		}

		public override string ToString()
		{
			return Name;
		}

		public static bool operator ==(UserBattleStatus left, UserBattleStatus right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(UserBattleStatus left, UserBattleStatus right)
		{
			return !Equals(left, right);
		}
	} ;
}