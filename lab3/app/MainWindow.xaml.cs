using System.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lab3
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private int[] square_sizes = null!;
        private int LENGTH_CHROM;
        private int POLE_SIZE;
        private int SQUARES;
        private Individual[] population = null!;
        private int[] ideal_gen = null!;
        private PlotModel plotModel = null!;
        private CancellationTokenSource cancellationTokenSource = null!;
        private bool workInProgress = false; 
        private int StartGeneration = 0;
        private int Ideal_fitness = -1;
        private int population_size = 500;
        private int num1x1 = 3;
        private int num2x2 = 2;
        private int num3x3 = 1;
        private bool start_or_load_clicked = false;
        private CancellationToken token;
        public int NumberOf1x1 
        {
            get {return num1x1;}
            set
            {
                num1x1 = value;
                OnPropertyChanged("NumberOf1x1");
            }
        }
        public int NumberOf2x2 
        {
            get {return num2x2;}
            set
            {
                num2x2 = value;
                OnPropertyChanged("NumberOf2x2");
            }
        }
        public int NumberOf3x3
        {
            get {return num3x3;}
            set
            {
                num3x3 = value;
                OnPropertyChanged("NumberOf3x3");
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        public class GeneticAlgorithmData
        {
            public int generations {get; set;}
            public int[][]? population_genes {get; set;}
            public int[]? population_fitnesses {get; set;}
            public int fitness {get; set;}
            public int[]? gen {get; set;}
            public int n_1x1 {get; set;}
            public int n_2x2 {get; set;}
            public int n_3x3 {get; set;}
        }
        public void JsonLog(string runname, int current_generations, Individual[] current_population, int cur_ideal_fitness, int[] cur_ideal_gen, int _1x1, int _2x2, int _3x3)
        {
            var current_population_genes = new int[population_size][];
            var current_population_fitnesses = new int[population_size];
            for (var i = 0; i < population_size; i++)
            {
                current_population_genes[i] = new int[LENGTH_CHROM];
                current_population_genes[i] = current_population[i].genes;
                current_population_fitnesses[i] = current_population[i].fitness;
            }
            var data = new GeneticAlgorithmData
            {
                generations = current_generations,
                population_genes = current_population_genes,
                population_fitnesses = current_population_fitnesses,
                fitness = cur_ideal_fitness,
                gen = cur_ideal_gen,
                n_1x1 = _1x1,
                n_2x2 = _2x2,
                n_3x3 = _3x3
            };
            string old_json = File.ReadAllText("../data.json");
            File.Copy("../data.json", "../data_copy.json");
            Dictionary<string, GeneticAlgorithmData> all_data;
            if (old_json.Length != 0)
                all_data = JsonConvert.DeserializeObject<Dictionary<string, GeneticAlgorithmData>>(old_json)!;
            else
                all_data = new Dictionary<string, GeneticAlgorithmData>();
            all_data[runname] = data;
            string json = JsonConvert.SerializeObject(all_data);
            File.WriteAllText("../data.json", json);
            File.Delete("../data_copy.json");
        }
        public async void JsonRead(string name)
        {
            string json = File.ReadAllText("../data.json");
            var d = JsonConvert.DeserializeObject<Dictionary<string, GeneticAlgorithmData>>(json);
            if (d!.ContainsKey(name))
            {
                var experimentData = d[name];
                NumberOf1x1 = experimentData.n_1x1;
                NumberOf2x2 = experimentData.n_2x2;
                NumberOf3x3 = experimentData.n_3x3;
                SQUARES = NumberOf1x1 + NumberOf2x2 + NumberOf3x3;
                LENGTH_CHROM = 2 * SQUARES;
                POLE_SIZE = (int)Math.Sqrt(NumberOf1x1 + NumberOf2x2 * 4 + NumberOf3x3 * 9) * 3;
                square_sizes = 
                    Enumerable.Range(0, NumberOf1x1).Select(_ => 1).Concat(
                        Enumerable.Range(0, NumberOf2x2).Select(_ => 2).Concat(
                            Enumerable.Range(0, NumberOf3x3).Select(_ => 3))).ToArray();
                StartGeneration = experimentData.generations;
                population = new Individual[population_size];
                var population_genes_ = new int[population_size][];
                for (var i = 0; i < population_size; i++)
                    population_genes_[i] = new int[LENGTH_CHROM];
                population_genes_ = experimentData.population_genes;
                var population_fitnesses_ = new int[population_size];
                population_fitnesses_ = experimentData.population_fitnesses;
                for (var i = 0; i < population_size; i++)
                {
                    population[i] = new Individual(LENGTH_CHROM, POLE_SIZE);
                    population[i].genes = population_genes_![i];
                    population[i].fitness = population_fitnesses_![i];
                }
                Ideal_fitness = experimentData.fitness;
                ideal_gen = experimentData.gen!;
            }
            workInProgress = true;
            cancellationTokenSource = new CancellationTokenSource();
            token = cancellationTokenSource.Token;
            await Task.Factory.StartNew(() =>
            {
                GeneticStart();
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            workInProgress = false; 
            DrawSquares();
            BestFitnessText.Text = $"Best Loss: {Ideal_fitness}";
        }
        public bool CheckValidRunname(string? name)
        {
            string json = File.ReadAllText("../data.json");
            var d = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, dynamic>>>(json);
            if (json.Length != 0)
                foreach (var i in d!)
                    if (i.Key == name)
                        return false;
            return true;
        }
        public string[] GetNames()
        {
            string json = File.ReadAllText("../data.json");
            var d = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, dynamic>>>(json);
            List<string> ans = new List<string>();
            if (json.Length != 0)
                foreach (var i in d!)
                    ans.Add(i.Key);
            return ans.ToArray();
        }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            InitializePlot();
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
            start_or_load_clicked = true;
            StartGeneration = 0;
            Ideal_fitness = -1;
            SQUARES = NumberOf1x1 + NumberOf2x2 + NumberOf3x3;
            LENGTH_CHROM = 2 * SQUARES;
            POLE_SIZE = (int)Math.Sqrt(NumberOf1x1 + NumberOf2x2 * 4 + NumberOf3x3 * 9) * 3;
            square_sizes = 
                Enumerable.Range(0, NumberOf1x1).Select(_ => 1).Concat(
                    Enumerable.Range(0, NumberOf2x2).Select(_ => 2).Concat(
                        Enumerable.Range(0, NumberOf3x3).Select(_ => 3))).ToArray();
            population = GeneticAlgo.PopulationCreator(population_size, LENGTH_CHROM, POLE_SIZE);
            workInProgress = true;
            cancellationTokenSource = new CancellationTokenSource();
            token = cancellationTokenSource.Token;
            await Task.Factory.StartNew(() =>
            {
                GeneticStart();
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            workInProgress = false; 
            DrawSquares();
            BestFitnessText.Text = $"Best Loss: {Ideal_fitness}";
        }
        private void GeneticStart()
        {
            for (int generation = StartGeneration; generation < 1000; generation++)
            {
                if (token.IsCancellationRequested)
                {
                    Dispatcher.Invoke(() =>
                    {
                        string? runname = "";
                        while (true) 
                        {
                            if (runname != "")
                                MessageBox.Show("Experiment exists. Enter another name!");
                            InputDialog inputDialog = new InputDialog();
                            bool? result = inputDialog.ShowDialog();
                            if (result == true)
                            {
                                runname = inputDialog.InputText;
                                if (runname == "")
                                    MessageBox.Show("Name is empty. Enter the name!");
                                else
                                    if (CheckValidRunname(runname))
                                    {
                                        JsonLog(runname!, generation, population, Ideal_fitness, ideal_gen, NumberOf1x1, NumberOf2x2, NumberOf3x3);
                                        MessageBox.Show($"Experiment {runname} saved successsfully!");  
                                        break;
                                    }
                            }
                            else
                                break;
                        };
                    });
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
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!start_or_load_clicked)
                return;
            if (cancellationTokenSource != null)
                cancellationTokenSource.Cancel();
            DrawSquares();
            BestFitnessText.Text = $"Best Loss: {Ideal_fitness}";
        }
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (workInProgress)
                return;
            string[] names = GetNames();
            ExperimentSelection selectionWindow = new ExperimentSelection(names);
            if (selectionWindow.ShowDialog() == true)
            {
                start_or_load_clicked = true;
                string selectedExperiment = selectionWindow.SelectedExperiment!;
                MessageBox.Show($"Load experiment data: {selectedExperiment}");
                JsonRead(selectedExperiment);
            }
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