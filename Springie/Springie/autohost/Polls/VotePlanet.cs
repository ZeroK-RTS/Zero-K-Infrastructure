using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VotePlanet: AbstractPoll
    {
        int planetID;

        public VotePlanet(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;
            if (spring.IsRunning)
            {
                AutoHost.Respond(tas, spring, e, "Cannot attack a different planet while the game is running");
                return false;
            }
            else
            {
                if (words.Length > 0)
                {
                    // FIXME get list of valid planets
                    //string[] vals;
                    //int[] indexes;
                    //ah.FilterMaps(words, out vals, out indexes);
                    if (true)   //(vals.Length > 0)
                    {
                        try
                        {
                            planetID = System.Convert.ToInt32(words[0]);
                            question = string.Format("Attack planet {0} http://zero-k.info/Planetwars/Planet/{1} ?", planetID, planetID);   // FIXME get planet name
                            return true;
                        }
                        catch (System.FormatException ex)
                        {
                            AutoHost.Respond(tas, spring, e, "Invalid planet ID");
                            return false;
                        }
                    }
                    else
                    {
                        AutoHost.Respond(tas, spring, e, "Invalid planet for attack");
                        return false;
                    }
                }
                else
                {
                    AutoHost.Respond(tas, spring, e, "Please specify a planet");    // FIXME list possible planets
                    return false;
                }
            }
        }
		
        protected override void SuccessAction()
        {
            {
                //ah.ComPlanet(TasSayEventArgs.Default, new string[] { planetID });
            }
        }
    }
}