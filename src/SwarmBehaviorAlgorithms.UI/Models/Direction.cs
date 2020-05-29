using System;

namespace SwarmBehaviorAlgorithms.UI.Models
{
    public class Direction
    {
        public Direction(int x, int y)
        {
            if (x < -1 || x > 1) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < -1 || y > 1) throw new ArgumentOutOfRangeException(nameof(y));

            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public override string ToString() =>
            new ToStringBuilder(nameof(Direction)) {{nameof(X), X}, {nameof(Y), Y}}.ToString();
    }
}