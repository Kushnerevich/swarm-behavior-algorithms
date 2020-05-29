using SwarmBehaviorAlgorithms.UI.Models;

namespace SwarmBehaviorAlgorithms.UI.Services
{
    public interface IMovementService
    {
        void MoveRobot(Robot robot, int numberOfSteps);
    }
}
