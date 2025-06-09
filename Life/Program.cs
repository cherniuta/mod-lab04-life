using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Text.Json;
using System.Drawing.Imaging;
using ScottPlot;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        public bool IsAliveNext;

        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Count(x => x.IsAlive);
            IsAliveNext = IsAlive ? liveNeighbors == 2 || liveNeighbors == 3 : liveNeighbors == 3;
        }

        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        private readonly Random rand = new Random();

        public int Columns => Cells.GetLength(0);
        public int Rows => Cells.GetLength(1);

        public Board(int width, int height, int cellSize, double liveDensity = 0.1)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            InitializeCells();
            ConnectNeighbors();
            Randomize(liveDensity);
        }

        private void InitializeCells()
        {
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();
        }

        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells) cell.DetermineNextLiveState();
            foreach (var cell in Cells) cell.Advance();
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.AddRange(new[] {
                        Cells[xL, yT], Cells[x, yT], Cells[xR, yT],
                        Cells[xL, y], Cells[xR, y],
                        Cells[xL, yB], Cells[x, yB], Cells[xR, yB]
                    });
                }
            }
        }

        public int CountAliveCells()
        {
            return Cells.Cast<Cell>().Count(cell => cell.IsAlive);
        }

        public (int generations, int aliveCount) SimulateUntilStable(int maxGenerations = 1000, int stableThreshold = 5)
        {
            int generations = 0;
            int lastAliveCount = -1;
            int stableSteps = 0;

            while (generations < maxGenerations && stableSteps < stableThreshold)
            {
                Advance();
                generations++;
                int currentAlive = CountAliveCells();

                if (currentAlive == lastAliveCount)
                    stableSteps++;
                else
                    stableSteps = 0;

                lastAliveCount = currentAlive;
            }
            return (generations, lastAliveCount);
        }

        public void SaveToFile(string path)
        {
            using (var writer = new StreamWriter(path))
            {
                for (int y = 0; y < Rows; y++)
                {
                    var line = new StringBuilder();
                    for (int x = 0; x < Columns; x++)
                        line.Append(Cells[x, y].IsAlive ? '*' : ' ');
                    writer.WriteLine(line);
                }
            }
        }

        public static Board LoadFromFile(string path)
        {
            var lines = File.ReadAllLines(path);
            int columns = lines[0].Length;
            int rows = lines.Length;

            var board = new Board(columns, rows, 1, 0);
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    board.Cells[x, y].IsAlive = lines[y][x] == '*';
            return board;
        }
    }

    public class Figure
    {
        public string Name { get; set; }
        public string[] Pattern { get; set; }

        public static Figure[] LoadFigures()
        {
            return new[]
            {
                new Figure { Name = "Блок", Pattern = new[] { "**", "**" } },
                new Figure { Name = "Улей", Pattern = new[] { " ** ", "*  *", " ** " } },
                new Figure { Name = "Планер", Pattern = new[] { " * ", "  *", "***" } },
                new Figure { Name = "Корабль", Pattern = new[] { "** ", "* *", " **" } },
                new Figure { Name = "Глайдер", Pattern = new[] { " * ", "* *", "** " } }
            };
        }
    }

    public static class Analysis
    {
        public static void GenerateStabilizationData(string outputPath = "Life/data.txt",
                                                   string plotPath = "Life/plot.png")
        {
            var densities = Enumerable.Range(1, 20)
                                   .Select(x => x * 0.05)
                                   .ToArray();

            var results = new List<string> { "Density,Generations,AliveCells" };

            foreach (var density in densities)
            {
                var board = new Board(100, 50, 1, density); 
                var (gens, alive) = board.SimulateUntilStable(maxGenerations: 500);
                results.Add($"{density:F2},{gens},{alive}");
            }

            File.WriteAllLines(outputPath, results);
            GeneratePlot(results, plotPath);
        }

        public static void GeneratePlot(List<string> data, string path)
        {
            var parsedData = data.Skip(1)
                               .Select(x => x.Split(','))
                               .ToList();

            double[] densities = parsedData.Select(x => double.Parse(x[0])).ToArray();
            double[] generations = parsedData.Select(x => double.Parse(x[1])).ToArray();

            var plot = new Plot();
            var scatter = plot.Add.Scatter(densities, generations);
            scatter.LineWidth = 3;
            scatter.Color = Colors.Blue;
            scatter.MarkerSize = 8;

            plot.XLabel("Плотность заполнения", size: 16);
            plot.YLabel("Поколения до стабилизации", size: 16);
            plot.Title("Анализ стабилизации игры 'Жизнь'", size: 20);

            plot.Axes.Bottom.Min = -1;
            plot.Axes.Bottom.Max = 1;
            plot.Axes.Left.Min = -50;
            plot.Axes.Left.Max = generations.Max() * 1.1;
            plot.Axes.Margins(0, 0.1);

            int maxIndex = generations.IndexOfMax();
            plot.Add.Marker(densities[maxIndex], generations[maxIndex],
                          color: Colors.Red, size: 15, shape: MarkerShape.OpenCircle);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            plot.SavePng(path, 1000, 600);
        }

        private static int IndexOfMax(this double[] array)
        {
            double max = array[0];
            int index = 0;
            for (int i = 1; i < array.Length; i++)
            {
                if (array[i] > max)
                {
                    max = array[i];
                    index = i;
                }
            }
            return index;
        }
    }


    public class Settings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int CellSize { get; set; }
        public double LiveDensity { get; set; }
    }

    class Program
    {
        static Board board;

        static void Reset()
        {
            var settings = LoadSettings();
            board = new Board(settings.Width, settings.Height, settings.CellSize, settings.LiveDensity);
        }

        static Settings LoadSettings()
        {
            var path = "settings.json";
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Settings>(json);
            }
            return new Settings { Width = 50, Height = 20, CellSize = 1, LiveDensity = 0.5 };
        }

        static void Render()
        {
            for (int y = 0; y < board.Rows; y++)
            {
                for (int x = 0; x < board.Columns; x++)
                    Console.Write(board.Cells[x, y].IsAlive ? '*' : ' ');
                Console.WriteLine();
            }
        }

        static void Main()
        {
            Reset();
            Analysis.GenerateStabilizationData();

            var figures = Figure.LoadFigures();
            var counts = new Dictionary<string, int>();
            foreach (var fig in figures)
                counts[fig.Name] = 0;

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.S:
                            board.SaveToFile("state.txt");
                            Console.WriteLine("Сохранено в state.txt");
                            Thread.Sleep(1000);
                            break;
                        case ConsoleKey.L:
                            if (File.Exists("state.txt"))
                            {
                                board = Board.LoadFromFile("state.txt");
                                Console.WriteLine("Загружено из state.txt");
                                ClassifyFigures(board, figures, counts);
                                Thread.Sleep(1000);
                            }
                            break;
                        case ConsoleKey.R:
                            Reset();
                            Console.WriteLine("Сброс");
                            Thread.Sleep(1000);
                            break;
                        case ConsoleKey.Escape:
                            return;
                    }
                }

                Console.Clear();
                Render();
                board.Advance();
                Thread.Sleep(300);
            }
        }

        static void ClassifyFigures(Board board, Figure[] figures, Dictionary<string, int> counts)
        {
            foreach (var fig in figures)
            {
                if (FigureMatches(board, fig.Pattern))
                    counts[fig.Name]++;
            }
            Console.WriteLine("Найдены фигуры:");
            foreach (var entry in counts)
                Console.WriteLine($"{entry.Key}: {entry.Value}");
        }

        static bool FigureMatches(Board board, string[] pattern)
        {
            return false; 
        }
    }
}