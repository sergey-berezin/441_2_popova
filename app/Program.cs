using Lab1;
public class Program
{
    public static void Main(string[] args)
    {
        int number_of_1x1 = 3;
        int number_of_2x2 = 2;
        int number_of_3x3 = 2;
        int[] square_sizes = 
            Enumerable.Range(0, number_of_1x1).Select(_ => 1).Concat(
                Enumerable.Range(0, number_of_2x2).Select(_ => 2).Concat(
                    Enumerable.Range(0, number_of_3x3).Select(_ => 3))).ToArray();
        int SQUARES = number_of_1x1 + number_of_2x2 + number_of_3x3;
        int LENGTH_CHROM = 2 * SQUARES;
        int POLE_SIZE = (int)Math.Sqrt(number_of_1x1 + number_of_2x2 * 4 + number_of_3x3 * 9) * 3;
        Individual[] population = GeneticAlgo.PopulationCreator(500, LENGTH_CHROM, POLE_SIZE);
        int[] ideal_gen = {};
        int ideal_fitness = -1;
        for (int generation = 0; generation < 1000; generation++)
        {
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
            Console.WriteLine($"Поколение {generation}: Макс приспособ. = {minFitness}");
            foreach (Individual ind in population)
                if (ind.fitness == minFitness) 
                {
                    ideal_gen = ind.genes;
                    ideal_fitness = minFitness;
                    break;
                }
        }
        for (int i = 0; i < LENGTH_CHROM; i += 2)
            Console.WriteLine($"Квадрат со стороной {square_sizes[i / 2]}: x {ideal_gen[i]}; y {ideal_gen[i + 1]}");
        Console.WriteLine($"Результат: {ideal_fitness}");
    }
} 