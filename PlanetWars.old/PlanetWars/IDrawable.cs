using System.Drawing;

namespace PlanetWars
{
    interface IDrawable
    {
        void Draw(Graphics g, Size mapSize);
    }
}