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
        public void Analysis_EmptyBoard_StabilizesImmediately()
        {
            var board = new Board(20, 20, 1, 0);
            var (gens, alive) = board.SimulateUntilStable();
            Assert.AreEqual(6, gens);
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
        public void Analysis_StabilizationData_GeneratesValidCSV()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "LifeTest");
            Directory.CreateDirectory(tempDir);
            var outputPath = Path.Combine(tempDir, "test_output.csv");
            var plotPath = Path.Combine(tempDir, "plot.png");

            try
            {
                Analysis.GenerateStabilizationData(outputPath, plotPath);
                var lines = File.ReadAllLines(outputPath);
                Assert.Greater(lines.Length, 1);
                Assert.IsTrue(lines[0].Contains("Density"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void Analysis_MediumDensity_TakesLongestToStabilize()
        {
            var densities = new[] { 0.05, 0.2, 0.5, 0.8, 0.95 };
            var results = densities.Select(d => {
                var board = new Board(50, 50, 1, d);
                return board.SimulateUntilStable(maxGenerations: 2000).generations;
            }).ToList();

            Assert.Greater(results[2], results[0]);
            Assert.Greater(results[2], results[4]); 
        }

        [Test]
        public void Cell_AliveWith2Neighbors_StaysAlive()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 2));
            cell.DetermineNextLiveState();
            Assert.IsTrue(cell.IsAliveNext);
        }

        [Test]
        public void Cell_AliveWith3Neighbors_StaysAlive()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 3));
            cell.DetermineNextLiveState();
            Assert.IsTrue(cell.IsAliveNext);
        }

        [Test]
        public void Board_EmptyBoard_NoAliveCells()
        {
            var board = new Board(10, 10, 1, 0);
            Assert.AreEqual(0, board.CountAliveCells());
        }

        [Test]
        public void Board_FullBoard_AllCellsAlive()
        {
            var board = new Board(10, 10, 1, 1.0);
            Assert.AreEqual(100, board.CountAliveCells());
        }

        [Test]
        public void Board_Advance_ChangesState()
        {
            var board = new Board(10, 10, 1, 0.5);
            int before = board.CountAliveCells();
            board.Advance();
            int after = board.CountAliveCells();
            Assert.AreNotEqual(before, after);
        }
    }
}