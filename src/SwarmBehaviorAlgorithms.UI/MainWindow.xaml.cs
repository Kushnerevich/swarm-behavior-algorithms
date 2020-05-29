using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveCharts;
using MahApps.Metro.Controls;
using ReactiveUI;
using SwarmBehaviorAlgorithms.UI.Models;
using SwarmBehaviorAlgorithms.UI.ViewModels;

namespace SwarmBehaviorAlgorithms.UI
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, IViewFor<AppViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new AppViewModel();
            DataContext = ViewModel;

            this.WhenActivated(disposableRegistration =>
            {
                this.BindCommand(ViewModel, vm => vm.AssessCommand, v => v.AssessButton)
                    .DisposeWith(disposableRegistration);

                this.BindCommand(ViewModel, vm => vm.GenerateCargoesWithTargets, v => v.AddCargoesWithTargetsButton)
                    .DisposeWith(disposableRegistration);

                ViewModel.WhenAnyValue(x => x.Cargoes, x => x.Targets)
                    .Where((items) => items.Item1 != null && items.Item2 != null)
                    .Subscribe((items) =>
                    {
                        CanvasControl.Children.Clear();
                        foreach (var cargo in items.Item1) DrawCargo(cargo);

                        foreach (var target in items.Item2) DrawTarget(target);
                    });
            });
        }

        private bool _isRun;

        private async void RunSimulation_OnClick(object sender, RoutedEventArgs e)
        {
            await Task.Yield();

            ViewModel.ActualNumberOfIterations = 0;

            _isRun = true;
            var delay = TimeSpan.FromMilliseconds(ViewModel.AnimationDelay);

            CleanupCargosAndTargets();
            foreach (var cargo in ViewModel.Cargoes)
            {
                cargo.IsAssigned = true;
            }

            for (var i = 0; i < 5000; i++)
            {
                if (ViewModel.Targets.All(x => x.IsDelivered) || !_isRun)
                {
                    return;
                }

                ViewModel.ActualNumberOfIterations++;

                await Task.Delay(delay);

                for (var j = 0; j < ViewModel.NumberOfMoves; j++)
                {
                    ViewModel.MoveRobots(ViewModel.Robots, j == ViewModel.NumberOfMoves - 1);
                    Dispatcher?.Invoke(() => ReDrawAllObjects());
                }
            }
        }

        private void DrawCargo(Cargo cargo)
        {
            const int triangleHeight = 20;

            var triangle = new Polygon
            {
                Stroke = cargo.IsTaken ? Brushes.Brown : Brushes.Black,
                Fill = cargo.IsTaken ? Brushes.White : Brushes.LightSeaGreen,
                StrokeThickness = 1
            };

            var p1 = new Point(cargo.Position.X, cargo.Position.Y - triangleHeight / 2);
            var p2 = new Point(cargo.Position.X - triangleHeight / 2, cargo.Position.Y + triangleHeight / 2);
            var p3 = new Point(cargo.Position.X + triangleHeight / 2, cargo.Position.Y + triangleHeight / 2);

            triangle.Points = new PointCollection {p1, p2, p3};

            var textBlock = new TextBlock
            {
                Text = cargo.Number.ToString(),
                Foreground = cargo.IsTaken ? Brushes.Black : Brushes.White,
                FontSize = 12
            };

            Canvas.SetLeft(textBlock, cargo.Position.X - triangleHeight / 6);
            Canvas.SetTop(textBlock, cargo.Position.Y - triangleHeight / 3);

            CanvasControl.Children.Add(triangle);
            CanvasControl.Children.Add(textBlock);
        }

        private void DrawRobots(IEnumerable<Robot> robots)
        {
            foreach (var robot in robots)
            {
                var circle = new Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = Brushes.Orange
                };

                Canvas.SetLeft(circle, robot.Position.X - 8);
                Canvas.SetTop(circle, robot.Position.Y - 8);

                var textBlock = new TextBlock
                {
                    Text = robot.TargetId.ToString(),
                    Foreground = Brushes.White,
                    FontSize = 10
                };

                Canvas.SetLeft(textBlock, robot.Position.X);
                Canvas.SetTop(textBlock, robot.Position.Y - 8);

                CanvasControl.Children.Add(circle);
                CanvasControl.Children.Add(textBlock);
            }
        }

        private void DrawTarget(Target target)
        {
            const int squareHeight = 20;

            var square = new Polygon
            {
                Stroke = Brushes.Aqua,
                Fill = Brushes.Black,
                StrokeThickness = 1
            };

            var p1 = new Point(target.Position.X + squareHeight / 2, target.Position.Y - squareHeight / 2);
            var p2 = new Point(target.Position.X - squareHeight / 2, target.Position.Y - squareHeight / 2);
            var p3 = new Point(target.Position.X - squareHeight / 2, target.Position.Y + squareHeight / 2);
            var p4 = new Point(target.Position.X + squareHeight / 2, target.Position.Y + squareHeight / 2);

            square.Points = new PointCollection {p1, p2, p3, p4};

            var textBlock = new TextBlock
            {
                Text = target.Number.ToString(),
                Foreground = Brushes.Chartreuse,
                FontSize = 12
            };

            Canvas.SetLeft(textBlock, target.Position.X - squareHeight / 6);
            Canvas.SetTop(textBlock, target.Position.Y - squareHeight / 3);

            CanvasControl.Children.Add(square);
            CanvasControl.Children.Add(textBlock);
        }

        private void CleanupCargosAndTargets()
        {
            foreach (var cargo in ViewModel.Cargoes)
            {
                cargo.IsAssigned = false;
                cargo.IsTaken = false;
            }

            foreach (var target in ViewModel.Targets) target.IsDelivered = false;
        }

        private void Arrangement_OnDataClick(object sender, ChartPoint chartPoint)
        {
            _isRun = false;
            ViewModel.Robots = ViewModel.ArrangementsResult[(int) chartPoint.X].Robots.Select(x =>
                    new Robot(new Models.Position(x.Position.X, x.Position.Y),
                        new Direction(x.Direction.X, x.Direction.Y))
                    {
                        IsStopped = false,
                        JobIsDone = false,
                        Target = x.Target
                    })
                .ToList();
            CleanupCargosAndTargets();
            ReDrawAllObjects();
        }

        private void Resource_OnDataClick(object sender, ChartPoint chartPoint)
        {
            _isRun = false;

            ViewModel.Robots = ViewModel.ResourcesResult[(int) chartPoint.X].Robots;
            CleanupCargosAndTargets();
            ReDrawAllObjects();
        }

        private void ReDrawAllObjects()
        {
            CanvasControl.Children.Clear();

            foreach (var cargo in ViewModel.Cargoes) DrawCargo(cargo);

            foreach (var target in ViewModel.Targets) DrawTarget(target);
            DrawRobots(ViewModel.Robots);
        }

        private void SearchType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.Robots != null && ViewModel.Robots.Any()) ViewModel.Robots = new List<Robot>();

            CleanupCargosAndTargets();
            ReDrawAllObjects();
        }

        /// <summary>The view model dependency property.</summary>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(AppViewModel), typeof(MainWindow), new PropertyMetadata(null));

        /// <summary>Gets the binding root view model.</summary>
        public AppViewModel BindingRoot => ViewModel;

        /// <inheritdoc />
        public AppViewModel ViewModel
        {
            get => (AppViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (AppViewModel) value;
        }
    }
}
