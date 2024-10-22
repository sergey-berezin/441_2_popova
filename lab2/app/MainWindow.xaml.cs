using System.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using System.Windows.Threading;

namespace Lab2
{
    public partial class MainWindow : Window
    {
        private int[] square_sizes = null!;
        private int LENGTH_CHROM;
        private int POLE_SIZE;
        private Individual[] population = null!;
        private int[] ideal_gen = null!;
        private PlotModel plotModel = null!;
        private CancellationTokenSource cancellationTokenSource = null!;
        private bool workInProgress = false; 

        public int Ideal_fitness {get; set;}
        public int NumberOf1x1 {get; set;}
        public int NumberOf2x2 {get; set;}
        public int NumberOf3x3 {get; set;}

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            InitializePlot();
            NumberOf1x1 = 3;
            NumberOf2x2 = 2;
            NumberOf3x3 = 1;
        }

        private void InitializePlot()
        {
            plotModel = new PlotModel { Title = "Squares Visualization" };

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 10,
                IsPanEnabled = true,
                IsZoomEnabled = true
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 10,
                IsPanEnabled = true,
                IsZoomEnabled = true
            });

            plotView.Model = plotModel;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (workInProgress)
                return;
            workInProgress = true;
            square_sizes = 
                Enumerable.Range(0, NumberOf1x1).Select(_ => 1).Concat(
                    Enumerable.Range(0, NumberOf2x2).Select(_ => 2).Concat(
                        Enumerable.Range(0, NumberOf3x3).Select(_ => 3))).ToArray();
            int SQUARES = NumberOf1x1 + NumberOf2x2 + NumberOf3x3;
            LENGTH_CHROM = 2 * SQUARES;
            POLE_SIZE = (int)Math.Sqrt(NumberOf1x1 + NumberOf2x2 * 4 + NumberOf3x3 * 9) * 3;
            population = GeneticAlgo.PopulationCreator(500, LENGTH_CHROM, POLE_SIZE);
            Ideal_fitness = -1;
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            await Task.Factory.StartNew(() =>
            {
                for (int generation = 0; generation < 1000; generation++)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    Individual[] offspring = GeneticAlgo.SelTournament(population, population.Length);
                    for (int i = 0; i < offspring.Length; i += 2)
                        if (new Random().NextDouble() < 0.9)
                        {
                            var children = GeneticAlgo.Crossover(offspring[i], offspring[i + 1]);
                            offspring[i] = children.Item1;
                            offspring[i + 1] = children.Item2;
                        }
                    for (int i = 0; i < offspring.Length; i++)
                        if (new Random().NextDouble() < 0.4)
                            offspring[i].Mutate(1.0 / LENGTH_CHROM, POLE_SIZE);
                    foreach (Individual ind in offspring)
                        ind.fitness = ind.Loss(square_sizes, POLE_SIZE);
                    population = offspring;
                    int minFitness = population.Min(ind => ind.fitness);
                    foreach (Individual ind in population)
                        if (ind.fitness == minFitness) 
                        {
                            ideal_gen = ind.genes;
                            Ideal_fitness = minFitness;
                            break;
                        }
                    Dispatcher.Invoke(() =>
                    {
                        DrawSquares();
                        BestFitnessText.Text = $"Current Loss: {Ideal_fitness}";
                    });
                    Thread.Sleep(3);
                }
                workInProgress = false; 
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            DrawSquares();
            BestFitnessText.Text = $"Best Loss: {Ideal_fitness}";
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
            DrawSquares();
            BestFitnessText.Text = $"Best Loss: {Ideal_fitness}";
        }

        private void DrawSquares()
        {
            plotModel.Annotations.Clear();
            int min_x = int.MaxValue, min_y = int.MaxValue;
            int max_x = int.MinValue, max_y = int.MinValue;

            for (int i = 0; i < LENGTH_CHROM; i += 2)
            {
                min_x = Math.Min(ideal_gen[i], min_x);
                min_y = Math.Min(ideal_gen[i + 1], min_y);
                max_x = Math.Max(ideal_gen[i] + square_sizes[i / 2], max_x);
                max_y = Math.Max(ideal_gen[i + 1] + square_sizes[i / 2], max_y);
            }

            for (int i = 0; i < LENGTH_CHROM; i += 2)
            {
                int x = ideal_gen[i];
                int y = ideal_gen[i + 1];
                int size = square_sizes[i / 2];

                OxyColor fillColor = square_sizes[i / 2] switch
                {
                    1 => OxyColors.LightGreen,
                    2 => OxyColors.LightSkyBlue,
                    3 => OxyColors.Plum,
                    _ => OxyColors.Black
                };

                plotModel.Annotations.Add(
                    new RectangleAnnotation
                    {
                        MinimumX = x - min_x + 1,
                        MaximumX = x - min_x + size + 1,
                        MinimumY = y - min_y + 1,
                        MaximumY = y - min_y + size + 1,
                        Fill = fillColor,
                        Stroke = OxyColors.Black,
                        StrokeThickness = 1
                    }
                );
            }
            plotModel.Annotations.Add(
                new RectangleAnnotation
                {
                    MinimumX = 1,
                    MaximumX = max_x - min_x + 1,
                    MinimumY = 1,
                    MaximumY = max_y - min_y + 1,
                    Fill = OxyColors.Transparent,
                    Stroke = OxyColors.Black,
                    StrokeThickness = 1
                }
            );
            plotView.InvalidatePlot(true);
        }
    }
}
