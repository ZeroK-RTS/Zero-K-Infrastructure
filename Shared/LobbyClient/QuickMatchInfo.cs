using LobbyClient;

namespace LobbyClient
{
    /// <summary>
    /// Holds information about users quickmatch settings
    /// </summary>
    public class QuickMatchInfo
    {
        public BattleMode CurrentMode { get; private set; }
        public string GameName { get; private set; }
        public bool IsEnabled { get; private set; }
        public bool IsSpectating { get; private set; }
        public int MinPlayers { get; private set; }
        public QuickMatchInfo() {}

        public QuickMatchInfo(string gameName, int limit, BattleMode mode, bool isSpectating)
        {
            IsEnabled = true;
            GameName = gameName;
            MinPlayers = limit;
            CurrentMode = mode;
            IsSpectating = isSpectating;
        }

        public bool Equals(QuickMatchInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.CurrentMode, CurrentMode) && Equals(other.GameName, GameName) && other.IsEnabled.Equals(IsEnabled) &&
                   other.IsSpectating.Equals(IsSpectating) && other.MinPlayers == MinPlayers;
        }

        public static QuickMatchInfo FromTasClientString(string data)
        {
            if (!data.StartsWith("#QM#")) return null;
            data = data.Substring(4);
            if (data == "_:0") return new QuickMatchInfo();
            else
            {
                var parts = data.Split(':');
                if (parts.Length >= 2)
                {
                    var game = parts[0];
                    int count;
                    int.TryParse(parts[1], out count);
                    var mode = BattleMode.QuickMatch;
                    var isSpectating = false;
                    if (parts.Length > 2)
                    {
                        if (parts[2] == "F") mode = BattleMode.Follow;
                        else if (parts[2] == "N") mode = BattleMode.Normal;
                        else if (parts[2] == "S")
                        {
                            mode = BattleMode.QuickMatch;
                            isSpectating = true;
                        }
                    }
                    return new QuickMatchInfo(game, count, mode, isSpectating);
                }
                else return new QuickMatchInfo();
            }
        }

        public string ToTasClientString()
        {
            if (!IsEnabled) return string.Format("#QM#_:0");

            var c = "_";
            if (CurrentMode == BattleMode.QuickMatch)
            {
                if (IsSpectating) c = "S";
                else c = "Q";
            }
            else if (CurrentMode == BattleMode.Follow) c = "F";
            else if (CurrentMode == BattleMode.Normal) c = "N";

            return string.Format("#QM#{0}:{1}:{2}", GameName, MinPlayers, c);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(QuickMatchInfo)) return false;
            return Equals((QuickMatchInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = CurrentMode.GetHashCode();
                result = (result*397) ^ (GameName != null ? GameName.GetHashCode() : 0);
                result = (result*397) ^ IsEnabled.GetHashCode();
                result = (result*397) ^ IsSpectating.GetHashCode();
                result = (result*397) ^ MinPlayers;
                return result;
            }
        }

        public override string ToString()
        {
            if (IsEnabled)
            {
							if (CurrentMode == BattleMode.QuickMatch) return string.Format("{2} {0} {1}", MinPlayers > 0 ? MinPlayers.ToString(): "", GameName, IsSpectating ? "Speccing" : "QuickMatch");
                else if (CurrentMode == BattleMode.Follow) return string.Format("following {0}", GameName);
                else return string.Format("waiting {0}", MinPlayers);
            }
            else return "";
        }

        public static bool operator ==(QuickMatchInfo left, QuickMatchInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(QuickMatchInfo left, QuickMatchInfo right)
        {
            return !Equals(left, right);
        }
    }
}