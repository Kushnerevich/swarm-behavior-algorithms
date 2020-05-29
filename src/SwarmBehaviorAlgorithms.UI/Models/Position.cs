namespace SwarmBehaviorAlgorithms.UI.Models
{
    public class Position
    {
        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public override string ToString() =>
            new ToStringBuilder(nameof(Position)) {{nameof(X), X}, {nameof(Y), Y}}.ToString();
    }
}