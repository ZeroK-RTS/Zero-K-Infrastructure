using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace PlanetWars.ServiceReference
{
    public class ServicePartialClasses {} // for ctrl-t

    public partial class Player {}

    public partial class Fleet {}

    public partial class StarMap
    {
        bool isInitialized;
        IDictionary<int, Player> players;

        public double GetGameTurn(double offsetSeconds)
        {
            var gameTurn = 0.0;

            if (Config.Started.HasValue) {
                var seconds = Config.LocalGameSecond - Config.Started.Value.Second;
                gameTurn = seconds/Config.SecondsPerTurn;
                gameTurn += offsetSeconds/Config.SecondsPerTurn;
            }
            return gameTurn;
        }

        public Player GetOwner(CelestialObject body)
        {
            if (!body.OwnerID.HasValue) return null;
            Player player;
            return players.TryGetValue(body.OwnerID.Value, out player) ? player : null;
        }

        public void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;
            Config.RetrievalTime = DateTime.Now;
            players = Players.ToDictionary(p => p.PlayerID);
            foreach (var star in CelestialObjects.Where(o => o.CelestialObjectType == CelestialObjectType.Star)) {
                star.StarMap = this;
                star.Size = 10;
                ProcessSatellites(star, 1, star);
            }
            foreach (var body in CelestialObjects) {
                body.Children = CelestialObjects.Where(o => o.ParentObject == body.CelestialObjectID).ToArray(); // todo: don't use linear search?
            }
        }

        void ProcessSatellites(CelestialObject parent, int orbitNestingLevel, CelestialObject star)
        {
            foreach (var body in CelestialObjects.Where(o => o.ParentObject == parent.CelestialObjectID)) {
                body.OrbitNestingLevel = orbitNestingLevel;
                body.Parent = parent;
                body.ParentStar = star;
                body.StarMap = this;
                ProcessSatellites(body, orbitNestingLevel + 1, star);
            }
        }
    }

    public partial class CelestialObject
    {
        public override string ToString()
        {
            return Name;
        }

        public Brush Brush
        {
            get
            {
                switch (CelestialObjectType) {
                    case CelestialObjectType.Planet:
                        return new SolidColorBrush(Colors.Cyan);
                    case CelestialObjectType.Moon:
                        return new SolidColorBrush(Colors.White);
                    case CelestialObjectType.Asteroid:
                        return new SolidColorBrush(Colors.Red);
                    case CelestialObjectType.Star:
                        return new SolidColorBrush(Colors.Yellow);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        public IEnumerable<CelestialObject> Children { get; set; }
        public Color Color
        {
            get
            {
                switch (CelestialObjectType) {
                    case CelestialObjectType.Planet:
                        return Colors.Cyan;
                    case CelestialObjectType.Moon:
                        return Colors.White;
                    case CelestialObjectType.Asteroid:
                        return Colors.Red;
                    case CelestialObjectType.Star:
                        return Colors.Yellow;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        public int OrbitNestingLevel { get; set; }
        public Player Owner
        {
            get { return StarMap.GetOwner(this); }
        }
        public string OwnerName
        {
            get { return Owner == null ? "Unclaimed" : Owner.Name; }
        }
        public CelestialObject Parent { get; set; }
        public CelestialObject ParentStar { get; set; }
        public StarMap StarMap { get; set; }
    }

    public partial class CelestialObjectShip
    {
        public ShipType ShipType
        {
            get { return App.ShipTypes[ShipTypeID]; }
        }
    }

    public partial class Config
    {
        public double LocalGameSecond
        {
            get { return GameSecond + (DateTime.Now - RetrievalTime).TotalSeconds; }
        }
        public DateTime RetrievalTime { get; set; }
    }

    public partial class CelestialObjectStructure
    {
        public StructureType StructureType
        {
            get { return App.StructureTypes[StructureTypeID]; }
        }
    }

    public partial class MothershipStructure
    {
        public StructureType StructureType
        {
            get { return App.StructureTypes[StructureTypeID]; }
        }
    }

    public partial class StructureOption
    {
        public bool IsBuildable
        {
            get { return CanBuild == BuildResponse.Ok; }
        }

        public void ForceRaisePropertyChanged(string propertyName)
        {
            RaisePropertyChanged(propertyName);
        }
    }

    public partial class ShipOption
    {
        public bool IsBuildable
        {
            get { return CanBuild == BuildResponse.Ok; }
        }

        public void ForceRaisePropertyChanged(string propertyName)
        {
            RaisePropertyChanged(propertyName);
        }
    }
}