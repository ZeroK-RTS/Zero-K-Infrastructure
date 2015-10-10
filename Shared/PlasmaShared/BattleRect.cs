#region using

using System;

#endregion

namespace PlasmaShared
{
    public class BattleRect
    {
        public const int Max = 200;

        int bottom; // values 0-200 (native tasclient format)
        int left; // values 0-200 (native tasclient format)
        int right; // values 0-200 (native tasclient format)
        int top; // values 0-200 (native tasclient format)

        public int Bottom { get { return bottom; } set { bottom = LimitByMax(value); } }

        public int Left { get { return left; } set { left = LimitByMax(value); } }

        public int Right { get { return right; } set { right = LimitByMax(value); } }

        public int Top { get { return top; } set { top = LimitByMax(value); } }

        public BattleRect() {}


        public BattleRect(double left, double top, double right, double bottom)
        {
            // convert from percentages
            this.left = LimitByMax((int)(Max*left));
            this.top = LimitByMax((int)(Max*top));
            this.right = LimitByMax((int)(Max*right));
            this.bottom = LimitByMax((int)(Max*bottom));
        }

        public BattleRect(int left, int top, int right, int bottom)
        {
            this.left = LimitByMax(left);
            this.top = LimitByMax(top);
            this.right = LimitByMax(right);
            this.bottom = LimitByMax(bottom);
        }

        public void ToFractions(out double left, out double top, out double right, out double bottom)
        {
            left = (double)Left/Max;
            top = (double)Top / Max;
            right = (double)Right / Max;
            bottom = (double)Bottom / Max;
        }

        
        static int LimitByMax(int input)
        {
            return Math.Min(Max, Math.Max(0, input));
        }
    } ;
}