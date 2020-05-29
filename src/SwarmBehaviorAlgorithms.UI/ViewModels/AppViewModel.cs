using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SwarmBehaviorAlgorithms.UI.Models;
using SwarmBehaviorAlgorithms.UI.Services;

namespace SwarmBehaviorAlgorithms.UI.ViewModels
{
    public class AppViewModel : ReactiveObject
    {
        [Reactive]
        public SearchType SearchType { get; set; } = SearchType.Bacterial;

        [Reactive]
        public int NumberOfCargos { get; set; } = 8;

        [Reactive]
        public int NumberOfMoves { get; set; }

        [Reactive]
        public int TimeLimit { get; set; } = 500;

        [Reactive]
        public int AnimationDelay { get; set; } = 1;

        [Reactive]
        public List<Robot> Robots { get; set; } = new List<Robot>();

        [Reactive]
        public List<Cargo> Cargoes { get; private set; } = new List<Cargo>();

        [Reactive]
        public List<Target> Targets { get; private set; } = new List<Target>();

        [Reactive]
        public int ActualNumberOfIterations { get; set; }

        public extern bool CanRunSimulation { [ObservableAsProperty] get; }

        public extern bool CanRunAssess { [ObservableAsProperty] get; }

        // todo: we don't want to store view dependencies in ViewModel. So, it will be refactored
        public extern SeriesCollection ArrangementSeriesCollection { [ObservableAsProperty] get; }
        public extern SeriesCollection ResourceSeriesCollection { [ObservableAsProperty] get; }

        public extern IList<(List<Robot> Robots, int Cycles)> ArrangementsResult { [ObservableAsProperty] get; }

        public extern IList<(List<Robot> Robots, int Cycles)> ResourcesResult { [ObservableAsProperty] get; }

        public ReactiveCommand<Unit, IList<(List<Robot> Robots, int Cycle)>> AssessArrangementCommand { get; }
        public ReactiveCommand<Unit, IList<(List<Robot> Robots, int Cycles)>> AssessResourcesCommand { get; }

        public CombinedReactiveCommand<Unit, IList<(List<Robot> Robots, int Cycles)>> AssessCommand { get; }

        public ReactiveCommand<Unit, Unit> GenerateCargoesWithTargets { get; }

        public AppViewModel()
        {
            this.WhenAnyValue(x => x.Robots)
                .Select(robots => robots?.Any() ?? false)
                .ToPropertyEx(this, x => x.CanRunSimulation);

            this.WhenAnyValue(x => x.SearchType)
                .Select(x => x switch
                {
                    SearchType.Bacterial => 10,
                    SearchType.FishSchool => 1,
                    _ => 1
                })
                .Subscribe(number => NumberOfMoves = number);

            this.WhenAnyValue(x => x.SearchType)
                .Select(x => x switch
                {
                    SearchType.Bacterial => 500,
                    SearchType.FishSchool => 5_000,
                    _ => 500
                })
                .Subscribe(timeLimit => TimeLimit = timeLimit);

            this.WhenAnyValue(x => x.Cargoes, x => x.Targets)
                .Select(x => (x.Item1?.Any() ?? false) && (x.Item2?.Any() ?? false))
                .ToPropertyEx(this, x => x.CanRunAssess);

            this.WhenAnyValue(x => x.ArrangementsResult)
                .Where(x => x != null)
                .Select(x => new SeriesCollection(Mappers.Xy<int>().X((value, idx) => idx).Y(value => value))
                {
                    new LineSeries
                    {
                        Values = new ChartValues<int>(x.Select(r => r.Cycles))
                    }
                })
                .ToPropertyEx(this, x => x.ArrangementSeriesCollection);

            this.WhenAnyValue(x => x.ResourcesResult)
                .Where(x => x != null)
                .Select(x => new SeriesCollection(Mappers.Xy<int>().X((value, idx) => idx).Y(value => value))
                {
                    new LineSeries
                    {
                        Values = new ChartValues<int>(x.Select(r => r.Robots.Count))
                    }
                })
                .ToPropertyEx(this, x => x.ResourceSeriesCollection);

            AssessArrangementCommand = ReactiveCommand.Create(() => Assess(() => NumberOfCargos, TimeLimit));

            AssessArrangementCommand.ToPropertyEx(this, x => x.ArrangementsResult);
            AssessResourcesCommand = ReactiveCommand.Create(() =>
                Assess(() => GetRandomNumber(NumberOfCargos, NumberOfCargos + 10), TimeLimit));
            AssessResourcesCommand.ToPropertyEx(this, x => x.ResourcesResult);

            AssessCommand = ReactiveCommand.CreateCombined(new[]
            {
                AssessArrangementCommand, AssessResourcesCommand
            });

            GenerateCargoesWithTargets = ReactiveCommand.Create(() =>
            {
                var cargoes = new List<Cargo>();
                var targets = new List<Target>();

                for (var i = 0; i < NumberOfCargos; i++)
                {
                    var target = new Target(i + 1, new Position(GetRandomNumber(), GetRandomNumber()));

                    var newCargo = new Cargo(i + 1, new Position(GetRandomNumber(), GetRandomNumber()), target);

                    targets.Add(target);
                    cargoes.Add(newCargo);
                }

                Robots = new List<Robot>();
                Cargoes = cargoes;
                Targets = targets;
                ActualNumberOfIterations = 0;
            });
        }

        private const int NumberOfRuns = 100;

        private IList<(List<Robot> Robots, int Cycle)> Assess(Func<int> numberOfRobotsFunc, int timeLimit)
        {
            var results = new List<(List<Robot> Robots, int Cycle)>();

            for (var run = 0; run < NumberOfRuns; run++)
            {
                CleanupCargosAndTargets(Cargoes, Targets);

                var robots = GenerateRobots(numberOfRobotsFunc());
                var initialRobots = robots.Select(x =>
                    new Robot(new Position(x.Position.X, x.Position.Y), new Direction(x.Direction.X, x.Direction.Y))
                    {
                        Target = x.Target
                    }).ToList();

                int i;
                for (i = 0; i < timeLimit; i++)
                {
                    for (var j = 0; j < NumberOfMoves; j++)
                        MoveRobots(robots, j == NumberOfMoves - 1);

                    if (Targets.All(t => t.IsDelivered))
                    {
                        results.Add((initialRobots, i));
                        break;
                    }
                }

                if (i == timeLimit) results.Add((initialRobots, timeLimit));
            }

            return results;
        }

        private void CleanupCargosAndTargets(IEnumerable<Cargo> cargoes, IEnumerable<Target> targets)
        {
            foreach (var cargo in cargoes)
            {
                cargo.IsAssigned = false;
                cargo.IsTaken = false;
            }

            foreach (var target in targets) target.IsDelivered = false;
        }

        public void MoveRobots(List<Robot> robots, bool isNewCycle)
        {
            // Move robots
            foreach (var robot in robots) MoveRobot(robot);

            if (isNewCycle)
            {
                foreach (var robot in robots)
                {
                    robot.IsStopped = false;
                    robot.Direction = new Direction(GetRandomNumber(-1, 1), GetRandomNumber(-1, 1));
                }
            }
        }

        private readonly IDictionary<SearchType, IMovementService> _movementServices =
            new Dictionary<SearchType, IMovementService>
            {
                {SearchType.Bacterial, new BacterialMovementService()},
                {SearchType.FishSchool, new FishSchoolMovementService()}
            };

        private void MoveRobot(Robot robot) => _movementServices[SearchType].MoveRobot(robot, NumberOfMoves);

        private List<Robot> GenerateRobots(int amount)
        {
            var robots = new List<Robot>();
            for (var i = 0; i < amount; i++)
            {
                var robot = new Robot(new Position(GetRandomNumber(), GetRandomNumber()),
                    new Direction(GetRandomNumber(-1, 1), GetRandomNumber(-1, 1)));
                robots.Add(robot);

                var cargoToAssign = GetRandomNumber(0, NumberOfCargos - 1);
                var cargoCounter = 0;

                while (Cargoes[cargoToAssign].IsAssigned && cargoCounter < NumberOfCargos)
                {
                    cargoToAssign = (cargoToAssign + 1) % NumberOfCargos;
                    cargoCounter++;
                }

                robot.Target = Cargoes[cargoToAssign];
                Cargoes[cargoToAssign].IsAssigned = true;
            }

            if (Cargoes.Any(c => !c.IsAssigned))
            {
                if (Debugger.IsAttached) Debugger.Break();
                throw new InvalidOperationException("Not all cargos were assigned");
            }

            if (robots.Any(r => r.Target == null))
            {
                if (Debugger.IsAttached) Debugger.Break();
                throw new InvalidOperationException("Not all robots have target");
            }

            return robots;
        }

        public static int GetRandomNumber(int minimum = 0, int maximum = FieldSize) =>
            Random.Next(minimum, maximum + 1);

        public const int FieldSize = 500;

        private static readonly Random Random = new Random((int) DateTime.UtcNow.Ticks);
    }
}
