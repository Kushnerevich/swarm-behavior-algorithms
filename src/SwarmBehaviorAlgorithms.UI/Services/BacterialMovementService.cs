using System;
using SwarmBehaviorAlgorithms.UI.Models;
using SwarmBehaviorAlgorithms.UI.ViewModels;

namespace SwarmBehaviorAlgorithms.UI.Services
{
    public class BacterialMovementService : IMovementService
    {
        public void MoveRobot(Robot robot, int numberOfSteps)
        {
            if (robot.IsStopped || robot.JobIsDone) return;

            var lastDist = robot.Distance;

            // если робот на текущем шаге не выходит за границы имитационного поля
            // по оси x или, если он уже за его пределами, не уходит ещё дальше
            if (robot.Position.X < AppViewModel.FieldSize && robot.Position.X > 0 ||
                robot.Position.X >= AppViewModel.FieldSize && robot.Direction.X != 1 ||
                robot.Position.X <= 0 && robot.Direction.X != -1)
                // делаем один шаг по оси x в установленном направлении
                robot.Position.X += numberOfSteps * robot.Direction.X;

            // если робот на текущем шаге не выходит за границы имитационного поля
            // по оси y или, если он уже за его пределами, не уходит ещё дальше
            if (robot.Position.Y < AppViewModel.FieldSize && robot.Position.Y > 0 ||
                robot.Position.Y >= AppViewModel.FieldSize && robot.Direction.Y != 1 ||
                robot.Position.Y <= 0 && robot.Direction.Y != -1)
                // делаем один шаг по оси y в установленном направлении
                robot.Position.Y += numberOfSteps * robot.Direction.Y;

            // если рассточние до цели увеличилось, останавливаем движение робота на текущем шаге имитации
            if (lastDist < robot.Distance) robot.IsStopped = true;

            // проверка на то, что робот достиг назначенной цели 
            if (Math.Abs(robot.Position.X - robot.Target.Position.X) < 10 &&
                Math.Abs(robot.Position.Y - robot.Target.Position.Y) < 10)
            {
                // если робот достиг груза, назаничить ему в качестве цели,
                // целевую точку, к которой был привязан данный груз
                if (robot.Target is Cargo cargo && cargo.IsTaken == false)
                {
                    cargo.IsTaken = true;
                    robot.Target = cargo.Target;
                }
                else if (robot.Target is Target target)
                {
                    // если достигнутая цель - целевая точка, остановить робота и 
                    // считать его работу выполненной
                    target.IsDelivered = true;
                    robot.JobIsDone = true;
                }
            }
        }
    }
}
