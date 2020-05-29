namespace SwarmBehaviorAlgorithms.UI.Models
{
    public class Cargo : ITarget
    {
        public Cargo(int number, Position position, Target target)
        {
            Number = number;
            Position = position;
            Target = target;
        }

        public bool IsTaken { get; set; }
        public bool IsAssigned { get; set; }
        public Target Target { get; }

        public int Number { get; }
        public Position Position { get; }

        public override string ToString() => new ToStringBuilder(nameof(Cargo))
            {{nameof(Target), Target}, {nameof(IsTaken), IsTaken}, {nameof(IsAssigned), IsAssigned}}.ToString();
    }
}