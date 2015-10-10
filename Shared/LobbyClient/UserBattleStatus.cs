#region using

using System;
using System.Net;
using Newtonsoft.Json;
using PlasmaShared;

#endregion

namespace LobbyClient
{
	public enum SyncStatuses
	{
		Unknown = 0,
		Synced = 1,
		Unsynced = 2
	}

	public class UserBattleStatus
	{
		public int AllyNumber;
		public bool IsSpectator;
        
        [JsonIgnore]
        public DateTime JoinTime = DateTime.Now;

		public string Name;
		
        
        public string ScriptPassword; // HACK reimplement this
		
        public SyncStatuses SyncStatus = SyncStatuses.Unknown;

		[JsonIgnore]
        public User LobbyUser;

		public int TeamNumber;


	    public void UpdateWith(UpdateUserBattleStatus u)
	    {
	        if (u != null) {
                if (u.Name != Name) throw new Exception(string.Format("Applying update of {0} to user {1}", u.Name, Name));
                if (u.AllyNumber.HasValue) AllyNumber = u.AllyNumber.Value;
                if (u.TeamNumber.HasValue) TeamNumber = u.TeamNumber.Value;
                if (u.IsSpectator.HasValue) IsSpectator = u.IsSpectator.Value;
                if (u.Sync.HasValue) SyncStatus = u.Sync.Value;
	        }
	    }

	    public UpdateUserBattleStatus ToUpdateBattleStatus()
	    {
	        return new UpdateUserBattleStatus() {
	            Name = Name,
	            AllyNumber = AllyNumber,
	            IsSpectator = IsSpectator,
	            Sync = SyncStatus,
	            TeamNumber = TeamNumber
	        };
	    }




		public UserBattleStatus() {}



		public UserBattleStatus(string name, User lobbyUser, string password= null)
		{
		    Name = name;
		    if (password != null) ScriptPassword = password;
		    else ScriptPassword = name;
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
			return other.AllyNumber == AllyNumber && other.IsSpectator.Equals(IsSpectator) &&
			       other.JoinTime.Equals(JoinTime) && Equals(other.Name, Name) &&
			       Equals(other.SyncStatus, SyncStatus) &&  other.TeamNumber == TeamNumber;
		}



		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof(UserBattleStatus)) return false;
			return Equals((UserBattleStatus)obj);
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


	    public PlayerTeam ToPlayerTeam()
	    {
	        return new PlayerTeam() {
	            AllyID = this.AllyNumber,
	            Name = this.Name,
	            LobbyID = this.LobbyUser.AccountID,
	            TeamID = this.TeamNumber,
	            IsSpectator = this.IsSpectator
	        };
	    }
	} ;
}