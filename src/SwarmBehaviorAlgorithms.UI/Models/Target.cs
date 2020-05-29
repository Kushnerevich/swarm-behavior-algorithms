namespace SwarmBehaviorAlgorithms.UI.Models
{
    public class Target : ITarget
    {
        public Target(int number, Position position)
        {
            Number = number;
            Position = position;
        }

        public int Number { get; }

        public bool IsDelivered { get; set; }

        public Position Position { get; }

        public override string ToString() =>
            new ToStringBuilder(nameof(Target)) {{nameof(Position), Position}, {nameof(IsDelivered), IsDelivered}}
                .ToString();
    }
}