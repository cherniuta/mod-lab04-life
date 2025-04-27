using NUnit.Framework;
using cli_life;
using System.IO;
using System.Linq;

namespace LifeTest
{
    [TestFixture]
    public class LifeTests
    {
        [Test]
        public void Cell_DeadWith3Neighbors_ComesToLife()
        {
            var cell = new Cell { IsAlive = false };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 3));
            cell.DetermineNextLiveState();
            Assert.IsTrue(cell.IsAliveNext);
        }

        [Test]
        public void Cell_AliveWithLessThan2Neighbors_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            Assert.IsFalse(cell.IsAliveNext);
        }

        [Test]
        public void Cell_AliveWithMoreThan3Neighbors_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 4));
            cell.DetermineNextLiveState();
            Assert.IsFalse(cell.IsAliveNext);
        }

        [Test]
        public void Board_Initialization_SetsCorrectDimensions()
        {
            var board = new Board(100, 50, 1);
            Assert.AreEqual(100, board.Columns);
            Assert.AreEqual(50, board.Rows);
        }

        [Test]
        public void Board_ConnectNeighbors_Creates8Neighbors()
        {
            var board = new Board(3, 3, 1);
            Assert.AreEqual(8, board.Cells[1, 1].neighbors.Count);
        }

        [Test]
        public void Board_Randomize_RespectsDensity()
        {
            var board = new Board(50, 50, 1);
            board.Randomize(0.3);
            int aliveCount = board.Cells.Cast<Cell>().Count(c => c.IsAlive);
            Assert.That(aliveCount, Is.InRange(600, 900)); 
        }

        [Test]
        public void Board_Advance_BlockPatternStaysStable()
        {
            var board = new Board(4, 4, 1);
            // Создаем блок
            board.Cells[1, 1].IsAlive = board.Cells[1, 2].IsAlive
                = board.Cells[2, 1].IsAlive = board.Cells[2, 2].IsAlive = true;

            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive && board.Cells[2, 2].IsAlive);
        }

        [Test]
        public void Board_SaveAndLoad_KeepsIdenticalState()
        {
            var board1 = new Board(10, 10, 1);
            board1.Randomize(0.4);
            board1.SaveToFile("test_save.txt");

            var board2 = Board.LoadFromFile("test_save.txt");
            bool identical = true;
            for (int x = 0; x < board1.Columns; x++)
                for (int y = 0; y < board1.Rows; y++)
                    if (board1.Cells[x, y].IsAlive != board2.Cells[x, y].IsAlive)
                        identical = false;
            Assert.IsTrue(identical);
        }

        [Test]
        public void Figure_LoadFigures_ContainsGlider()
        {
            var figures = Figure.LoadFigures();
            Assert.IsTrue(figures.Any(f => f.Name == "Глайдер"));
        }

        [Test]
        public void Figure_GliderPattern_HasCorrectShape()
        {
            var glider = Figure.LoadFigures().First(f => f.Name == "Глайдер");
            Assert.AreEqual(3, glider.Pattern.Length);
            Assert.AreEqual(3, glider.Pattern[2].Count(c => c == '*')); 
        }

        [Test]
        public void Analysis_EmptyBoard_StabilizesImmediately()
        {
            var board = new Board(20, 20, 1, 0);
            var (gens, alive) = board.SimulateUntilStable();
            Assert.AreEqual(0, gens);
            Assert.AreEqual(0, alive);
        }

        [Test]
        public void Analysis_FullBoard_StabilizesQuickly()
        {
            var board = new Board(20, 20, 1, 1.0);
            var (gens, alive) = board.SimulateUntilStable();
            Assert.Less(gens, 10);
        }

        [Test]
        public void Analysis_GeneratePlot_CreatesFile()
        {
            File.WriteAllLines("test_plot_data.csv",
                new[] { "Density,Generations", "0.1,5", "0.5,20" });

            Analysis.GeneratePlot(File.ReadAllLines("test_plot_data.csv").ToList(), "test_plot.png");
            Assert.IsTrue(File.Exists("test_plot.png"));
        }

        [Test]
        public void Analysis_StabilizationData_GeneratesValidCSV()
        {
            Analysis.GenerateStabilizationData("test_output.csv");
            var lines = File.ReadAllLines("test_output.csv");
            Assert.Greater(lines.Length, 1); 
            Assert.IsTrue(lines[0].Contains("Density")); 
        }

        [Test]
        public void Analysis_MediumDensity_TakesLongestToStabilize()
        {
            var densities = new[] { 0.1, 0.3, 0.5, 0.7, 0.9 };
            var results = densities.Select(d => {
                var board = new Board(30, 30, 1, d);
                return board.SimulateUntilStable().generations;
            }).ToList();

            Assert.Greater(results[2], results[0]);
            Assert.Greater(results[2], results[4]); 
        }
    }
}