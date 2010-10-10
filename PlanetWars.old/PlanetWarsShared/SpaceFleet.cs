using System;
using System.Drawing;

namespace PlanetWarsShared
{
    [Serializable]
    public class SpaceFleet
    {
        public const double SpeedPerTurn = 0.1;
        
        public PointF Start;
        public PointF Destination;
        public int StartTurn;
        public int Arrives;
        public int TargetPlanetID;
        public string OwnerName;

        public SpaceFleet() {}

        public string GetHumanReadableEta(Galaxy gal)
        {
            if (gal.Turn < Arrives) 
            return string.Format(
                "{0}`s blockade fleet.\nInbound to {1}\nETA: {2} turns", OwnerName,
                gal.GetPlanet(TargetPlanetID).Name,Arrives - gal.Turn);
            else return string.Format(
                "{0}`s blockade fleet.\nIn orbit of {1}\n", OwnerName,
                gal.GetPlanet(TargetPlanetID).Name);
        }

        
        public bool SetDestination(Galaxy gal, int targetPlanetID, out string message)
        {
            var planet = gal.GetPlanet(targetPlanetID);
            GetCurrentPosition(out Start, gal.Turn);
            TargetPlanetID = targetPlanetID;
            StartTurn = gal.Turn;
            Destination = planet.Position;
            Arrives = StartTurn + CalculateEta(Start, Destination);
            message = "ok";
            return true;
        }



        /// <summary>
        /// gets current position of the fleet
        /// </summary>
        /// <param name="curLoc">current location</param>
        /// <returns>true if in destination</returns>
        public bool GetCurrentPosition(out PointF curLoc, int turn)
        {
            if (turn >= Arrives) {
                curLoc = Destination;
                return true; 
            }
            double xd = Destination.X - Start.X;
            double yd = Destination.Y - Start.Y;
            double koef = 0;
            if (Arrives > StartTurn) koef = (double)(turn - StartTurn) / (Arrives - StartTurn);
            curLoc = new PointF((float)(Start.X + xd*koef), (float)(Start.Y + yd*koef));
            return false;
        }

        public bool IsAtDestination(int turn)
        {
            PointF p;
            return GetCurrentPosition(out p, turn);
        }


        public int EtaToPlanet(int planetID, Galaxy gal)
        {
            PointF curLoc;
            GetCurrentPosition(out curLoc, gal.Turn);
            return CalculateEta(curLoc, gal.GetPlanet(planetID).Position);
        }

        private static int CalculateEta(PointF start, PointF destination)
        {
            double xd = destination.X - start.X;
            double yd = destination.Y - start.Y;
            double distance = Math.Sqrt(xd*xd + yd*yd);
            return (int)Math.Ceiling((double)distance/SpeedPerTurn);
        }
       
    }
}