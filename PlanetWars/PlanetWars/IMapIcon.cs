using PlanetWars.ServiceReference;

namespace PlanetWars
{
    public interface IMapIcon: IUpdatable
    {
        CelestialObject Body { get; set; }
        IMapIcon ParentIcon { get; set; }
        double X { get; set; }
        double Y { get; set; }
    }
}