using System;
using owlgame;

namespace TetrisTests
{
    /// <summary>
    /// Scoring, level progression and the drop speed that follows the level.
    /// </summary>
    public class ScoreTests
    {
        private const int Lines = 35;

        private static void FillLine(GameBoard board, int line)
        {
            for (int j = GameBoard.WallThickness;
                 j < GameBoard.Columns - GameBoard.WallThickness; j++)
                board.Cells[line][j] = GameBoard.Settled;
        }

        private static void FillLines(GameBoard board, int first, int count)
        {
            for (int k = 0; k < count; k++)
                FillLine(board, first + k);
        }

        [TestCase(1, 100)]
        [TestCase(2, 300)]
        [TestCase(3, 500)]
        [TestCase(4, 800)]
        public void ScoreFollowsTheClassicTable(int lines, int expected)
        {
            GameBoard board = new GameBoard(Lines);
            FillLines(board, 20, lines);
            Assert.AreEqual(lines, board.ClearLines(), lines + " lines cleared");
            Assert.AreEqual(expected, board.Score, "points for " + lines + " line(s)");
        }

        [Test]
        public void ClearingOneLineAtATimeScoresLessThanFourAtOnce()
        {
            GameBoard fourAtOnce = new GameBoard(Lines);
            FillLines(fourAtOnce, 20, 4);
            fourAtOnce.ClearLines();

            GameBoard oneByOne = new GameBoard(Lines);
            for (int k = 0; k < 4; k++)
            {
                FillLine(oneByOne, 20);
                oneByOne.ClearLines();
            }

            Assert.AreEqual(800, fourAtOnce.Score, "four at once");
            Assert.AreEqual(400, oneByOne.Score, "four cleared one by one");
            Assert.IsTrue(fourAtOnce.Score > oneByOne.Score,
                "clearing several lines with one block must pay more");
        }

        [Test]
        public void LinesCounterAccumulates()
        {
            GameBoard board = new GameBoard(Lines);
            for (int k = 0; k < 3; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.AreEqual(3, board.Lines, "three lines counted");
        }

        [Test]
        public void NineLinesKeepLevelOne()
        {
            GameBoard board = new GameBoard(Lines);
            for (int k = 0; k < 9; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.AreEqual(9, board.Lines, "nine lines");
            Assert.AreEqual(1, board.Level, "still level one");
        }

        [Test]
        public void TenLinesReachLevelTwo()
        {
            GameBoard board = new GameBoard(Lines);
            for (int k = 0; k < 10; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.AreEqual(10, board.Lines, "ten lines");
            Assert.AreEqual(2, board.Level, "level two");
        }

        [Test]
        public void TwentyLinesReachLevelThree()
        {
            GameBoard board = new GameBoard(Lines);
            for (int k = 0; k < 20; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.AreEqual(3, board.Level, "level three after twenty lines");
        }

        [Test]
        public void PointsUseTheLevelBeforeTheUpgrade()
        {
            // This is the easy one to get wrong: award first, then level up.
            // Doing it the other way round pays the multiplier for a level the
            // player has not reached yet.
            GameBoard board = new GameBoard(Lines);
            for (int k = 0; k < 10; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.AreEqual(2, board.Level, "precondition: level two");

            int before = board.Score;
            FillLine(board, 20);
            board.ClearLines();

            Assert.AreEqual(200, board.Score - before,
                "level two pays 100 x 2 for a single line");
        }

        [Test]
        public void FourLinesAtLevelTwoPayEightHundredTimesTwo()
        {
            GameBoard board = new GameBoard(Lines);
            for (int k = 0; k < 10; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.AreEqual(2, board.Level, "precondition: level two");

            int before = board.Score;
            FillLines(board, 20, 4);
            board.ClearLines();

            Assert.AreEqual(1600, board.Score - before, "800 x level 2");
        }

        // ---------- drop speed ----------

        [Test]
        public void LevelOneUsesTheBaseInterval()
        {
            GameBoard board = new GameBoard(Lines);
            Assert.AreEqual(GameBoard.BaseDropInterval, board.DropInterval, "level 1 drops at 1000 ms");
        }

        [Test]
        public void LevelTwoIsFasterThanLevelOne()
        {
            GameBoard board = new GameBoard(Lines);
            int levelOne = board.DropInterval;
            for (int k = 0; k < 10; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.AreEqual(2, board.Level, "precondition: level two");
            Assert.IsTrue(board.DropInterval < levelOne,
                "level 2 interval " + board.DropInterval + " should be below " + levelOne);
        }

        [Test]
        public void DropIntervalFollowsTheFormulaUntilTheFloor()
        {
            GameBoard board = new GameBoard(Lines);
            for (int k = 0; k < 10; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            int expected = GameBoard.BaseDropInterval - (board.Level - 1) * GameBoard.DropIntervalStep;
            Assert.AreEqual(expected, board.DropInterval, "level 2 interval matches the formula");
        }

        [Test]
        public void DropIntervalIsClampedAtTheMinimum()
        {
            GameBoard board = new GameBoard(Lines);
            for (int k = 0; k < 120; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.IsTrue(board.Level >= 12, "plenty of levels reached, level=" + board.Level);
            Assert.AreEqual(GameBoard.MinDropInterval, board.DropInterval,
                "never faster than " + GameBoard.MinDropInterval + " ms");
        }

        [Test]
        public void DropIntervalClampsExactlyAtLevelEleven()
        {
            // The formula is 1000 - (level - 1) * 90, so level 10 gives 190 and
            // level 11 gives 100 - which is below the 120 floor. The clamp
            // therefore kicks in at level 11, and that crossing point used to be
            // untested (only level 1, 2 and the fully clamped 13 were).
            GameBoard board = new GameBoard(Lines);

            for (int k = 0; k < 90; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.AreEqual(10, board.Level, "90 lines is level 10");
            Assert.AreEqual(190, board.DropInterval, "level 10 still follows the formula");

            for (int k = 0; k < 10; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.AreEqual(11, board.Level, "100 lines is level 11");
            Assert.AreEqual(GameBoard.MinDropInterval, board.DropInterval,
                "level 11 formula would give 100, clamped up to " + GameBoard.MinDropInterval);

            for (int k = 0; k < 10; k++)
            {
                FillLine(board, 20);
                board.ClearLines();
            }
            Assert.AreEqual(12, board.Level, "110 lines is level 12");
            Assert.AreEqual(GameBoard.MinDropInterval, board.DropInterval,
                "still clamped at level 12");
        }

        [Test]
        public void ClearingNothingScoresNothing()
        {
            GameBoard board = new GameBoard(Lines);
            Assert.AreEqual(0, board.ClearLines(), "nothing to clear");
            Assert.AreEqual(0, board.Score, "no points for no line");
            Assert.AreEqual(1, board.Level, "no level change");
        }
    }
}
