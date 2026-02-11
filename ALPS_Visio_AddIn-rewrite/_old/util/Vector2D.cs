using System;

namespace VisioAddIn.util
{
    public class Vector2D
    {
        private readonly double x;
        private readonly double y;
        private const int NEAR_THRESHOLD = 20;

        public Vector2D(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public double getX()
        {
            return x;
        }
        public double getY()
        {
            return y;
        }

        public bool isNearTo(Vector2D toCheck)
        {
            return Math.Abs(x - toCheck.getX()) < NEAR_THRESHOLD
                   && Math.Abs(y - toCheck.getY()) < NEAR_THRESHOLD;
        }
    }
}
