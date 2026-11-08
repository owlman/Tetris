using System;
using System.Collections.Generic;
using owlgame;

namespace TetrisTests
{
    /// <summary>
    /// Board rules: walls, placement, movement, merging and line clearing.
    /// Everything here runs without a message loop.
    /// </summary>
    public class GameBoardTests
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

        private static bool AllWalls(GameBoard board)
        {
            for (int i = 0; i < board.LineCount; i++)
                for (int j = 0; j < GameBoard.Columns; j++)
                {
                    bool shouldBeWall = j < GameBoard.WallThickness
                        || j > GameBoard.Columns - 1 - GameBoard.WallThickness
                        || i > board.LineCount - 1 - GameBoard.FloorThickness;
                    if (shouldBeWall && board.CellAt(i, j) != GameBoard.Wall)
                        return false;
                }
            return true;
        }

        private static string Matrix(Block block)
        {
            string s = "";
            for (int i = 0; i < Block.MatrixSize; i++)
                for (int j = 0; j < Block.MatrixSize; j++)
                    s += block.Matrix[i, j];
            return s;
        }

        private static int CountSettled(GameBoard board)
        {
            int n = 0;
            for (int i = 0; i < board.LineCount; i++)
                for (int j = 0; j < GameBoard.Columns; j++)
                    if (board.CellAt(i, j) == GameBoard.Settled)
                        n++;
            return n;
        }

        // ---------- construction ----------

        [Test]
        public void NewBoardHasWallsAroundThePlayfield()
        {
            GameBoard board = new GameBoard(Lines);
            Assert.IsTrue(AllWalls(board), "walls and floor are in place");
            Assert.AreEqual(GameBoard.Wall, board.CellAt(0, 0), "top left corner is wall");
            Assert.AreEqual(GameBoard.Empty, board.CellAt(1, 5), "playfield starts empty");
        }

        [Test]
        public void NewBoardSpawnsCurrentAndNext()
        {
            GameBoard board = new GameBoard(Lines);
            Assert.IsNotNull(board.Current, "current block exists");
            Assert.IsNotNull(board.Next, "next block exists");
            Assert.AreEqual(0, board.Current.Row, "current block starts on the top line");
        }

        [Test]
        public void TooSmallBoardIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                delegate { new GameBoard(0); }, "zero lines");
            Assert.Throws<ArgumentOutOfRangeException>(
                delegate { new GameBoard(Block.MatrixSize); }, "one matrix high is not enough");
        }

        [Test]
        public void LevelAndScoreStartClean()
        {
            GameBoard board = new GameBoard(Lines);
            Assert.AreEqual(0, board.Score, "score starts at zero");
            Assert.AreEqual(0, board.Lines, "no lines cleared yet");
            Assert.AreEqual(1, board.Level, "level starts at one");
        }

        // ---------- CanPlace ----------

        [Test]
        public void CanPlaceAcceptsAnEmptyArea()
        {
            GameBoard board = new GameBoard(Lines);
            Assert.IsTrue(board.CanPlace(8, 1, board.Current.Matrix), "spawn area is free");
        }

        [Test]
        public void CanPlaceRejectsOutOfRangeColumns()
        {
            GameBoard board = new GameBoard(Lines);
            int[,] matrix = board.Current.Matrix;

            Assert.IsFalse(board.CanPlace(-1, 0, matrix), "negative column");
            Assert.IsFalse(board.CanPlace(GameBoard.Columns, 0, matrix), "column past the right edge");
        }

        [Test]
        public void CanPlaceRejectsOutOfRangeRowsWithoutThrowing()
        {
            // The original code indexed the board directly and only survived
            // because the floor happened to be hit first. Asking for a row below
            // the board used to throw IndexOutOfRangeException.
            GameBoard board = new GameBoard(Lines);
            int[,] matrix = board.Current.Matrix;

            Assert.DoesNotThrow(delegate { board.CanPlace(8, -1, matrix); }, "row -1");
            Assert.IsFalse(board.CanPlace(8, -1, matrix), "row -1 is rejected");

            Assert.DoesNotThrow(delegate { board.CanPlace(8, 999, matrix); }, "row far below");
            Assert.IsFalse(board.CanPlace(8, 999, matrix), "row far below is rejected");
        }

        [Test]
        public void CanPlaceRejectsTheWallColumns()
        {
            GameBoard board = new GameBoard(Lines);
            Assert.IsFalse(board.CanPlace(-1, 5, board.Current.Matrix),
                "column -1 overlaps the left wall");
        }

        [Test]
        public void CanPlaceRejectsOverlappingSettledCells()
        {
            GameBoard board = new GameBoard(Lines);

            // The filled cells of a block do not start at matrix row 0, so put the
            // obstacle exactly where this block would land instead of guessing a
            // line number.
            Block block = board.Current;
            const int row = 10;
            int occupiedLine = -1;
            for (int i = 0; i < Block.MatrixSize && occupiedLine < 0; i++)
                for (int j = 0; j < Block.MatrixSize; j++)
                    if (block.Matrix[i, j] == GameBoard.Settled)
                    {
                        occupiedLine = row + i;
                        break;
                    }

            Assert.IsTrue(occupiedLine > 0, "the block occupies at least one line");
            FillLine(board, occupiedLine);

            Assert.IsFalse(board.CanPlace(block.Column, row, block.Matrix),
                "cannot place over settled cells on line " + occupiedLine);
        }

        // ---------- movement ----------

        [Test]
        public void MoveDownAdvancesOneLine()
        {
            GameBoard board = new GameBoard(Lines);
            int before = board.Current.Row;
            Assert.IsTrue(board.MoveDown(1), "first step down succeeds");
            Assert.AreEqual(before + 1, board.Current.Row, "row increased by one");
        }

        [Test]
        public void MoveDownStopsAtTheFloor()
        {
            GameBoard board = new GameBoard(Lines);
            int guard = 0;
            while (board.MoveDown(1))
            {
                if (++guard > 100)
                    Assert.Fail("the block never landed");
            }
            Assert.IsFalse(board.MoveDown(1), "a landed block cannot move further");
        }

        [Test]
        public void MoveLeftAndRight()
        {
            GameBoard board = new GameBoard(Lines);
            int start = board.Current.Column;

            Assert.IsTrue(board.Move(true), "move right succeeds");
            Assert.AreEqual(start + 1, board.Current.Column, "column moved right");

            Assert.IsTrue(board.Move(false), "move left succeeds");
            Assert.AreEqual(start, board.Current.Column, "column moved back");
        }

        [Test]
        public void FailedMoveLeavesTheStateUntouched()
        {
            GameBoard board = new GameBoard(Lines);
            while (board.Move(false)) { }   // push it all the way left

            int column = board.Current.Column;
            int row = board.Current.Row;

            Assert.IsFalse(board.Move(false), "cannot move further left");
            Assert.AreEqual(column, board.Current.Column, "column unchanged after a failed move");
            Assert.AreEqual(row, board.Current.Row, "row unchanged after a failed move");
        }

        [Test]
        public void RotationEitherReplacesTheBlockOrLeavesItAlone()
        {
            // A random shape cannot be forced into a failing rotation reliably, so
            // drive several blocks to the floor (where rotation usually collides)
            // and check both outcomes:
            //   success -> a new block object, position kept
            //   failure -> the very same object, matrix untouched
            bool sawSuccess = false;
            bool sawFailure = false;

            for (int k = 0; k < 120; k++)
            {
                GameBoard board = new GameBoard(Lines);
                while (board.MoveDown(1)) { }

                Block before = board.Current;
                string matrixBefore = Matrix(before);
                int columnBefore = before.Column;
                int rowBefore = before.Row;

                if (board.Rotate())
                {
                    sawSuccess = true;
                    Assert.IsNotSame(before, board.Current,
                        "a successful rotation installs a new block object");
                    Assert.AreEqual(columnBefore, board.Current.Column, "rotation keeps the column");
                    Assert.AreEqual(rowBefore, board.Current.Row, "rotation keeps the row");
                }
                else
                {
                    sawFailure = true;
                    Assert.IsSame(before, board.Current,
                        "a failed rotation must not replace the block");
                    Assert.AreEqual(matrixBefore, Matrix(board.Current),
                        "a failed rotation must not change the matrix");
                }
            }

            Assert.IsTrue(sawSuccess, "expected at least one rotation to fit");
            Assert.IsTrue(sawFailure, "expected at least one rotation to be rejected");
        }

        // ---------- merge ----------

        [Test]
        public void MergeWritesTheBlockIntoTheBoard()
        {
            GameBoard board = new GameBoard(Lines);
            int before = CountSettled(board);
            board.Merge();
            Assert.AreEqual(before + 4, CountSettled(board), "merging adds exactly 4 settled cells");
        }

        // ---------- line clearing ----------

        [Test]
        public void FindFullLinesReportsCompleteLines()
        {
            GameBoard board = new GameBoard(Lines);
            Assert.Count(0, board.FindFullLines(), "empty board has no full line");

            FillLine(board, 10);
            List<int> full = board.FindFullLines();
            Assert.Count(1, full, "one complete line found");
            Assert.AreEqual(10, full[0], "the reported index is the filled one");
        }

        [Test]
        public void FindFullLinesReportsNonAdjacentLines()
        {
            GameBoard board = new GameBoard(Lines);
            FillLine(board, 10);
            FillLine(board, 12);

            List<int> full = board.FindFullLines();
            Assert.Count(2, full, "two non adjacent lines found");
            Assert.AreEqual(10, full[0], "first reported index");
            Assert.AreEqual(12, full[1], "second reported index");
        }

        [Test]
        public void ClearOneLine()
        {
            GameBoard board = new GameBoard(Lines);
            FillLine(board, 20);
            Assert.AreEqual(1, board.ClearLines(), "one line cleared");
            Assert.Count(0, board.FindFullLines(), "no line left behind");
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ClearSeveralAdjacentLines(int count)
        {
            GameBoard board = new GameBoard(Lines);
            FillLines(board, 20, count);
            Assert.AreEqual(count, board.ClearLines(), count + " lines cleared");
            Assert.Count(0, board.FindFullLines(), "no line left behind");
        }

        [Test]
        public void ClearNonAdjacentLines()
        {
            GameBoard board = new GameBoard(Lines);
            FillLine(board, 10);
            FillLine(board, 12);
            Assert.AreEqual(2, board.ClearLines(), "both lines cleared");
            Assert.Count(0, board.FindFullLines(), "no line left behind");
        }

        [Test]
        public void ClearingPushesTheLinesAboveDown()
        {
            GameBoard board = new GameBoard(Lines);
            // one marker cell well above the line that will vanish
            board.Cells[5][5] = GameBoard.Settled;
            FillLine(board, 20);

            board.ClearLines();

            Assert.AreEqual(GameBoard.Settled, board.CellAt(6, 5),
                "the marker moved down by one line");
            Assert.AreEqual(GameBoard.Empty, board.CellAt(5, 5),
                "nothing left at the old position");
        }

        [Test]
        public void TheFloorLineNeverClears()
        {
            GameBoard board = new GameBoard(Lines);
            int floor = Lines - 1;
            for (int j = 0; j < GameBoard.Columns; j++)
                board.Cells[floor][j] = GameBoard.Settled;

            Assert.Count(0, board.FindFullLines(), "the floor is not a clearable line");
            Assert.AreEqual(0, board.ClearLines(), "nothing cleared");
        }

        [Test]
        public void NewlyInsertedLinesCarryTheirWalls()
        {
            GameBoard board = new GameBoard(Lines);
            FillLine(board, 20);
            board.ClearLines();

            Assert.AreEqual(GameBoard.Wall, board.CellAt(0, 0), "left wall restored");
            Assert.AreEqual(GameBoard.Wall, board.CellAt(0, GameBoard.Columns - 1), "right wall restored");
            Assert.IsTrue(AllWalls(board), "all walls intact after a clear");
        }

        // ---------- turn flow ----------

        [Test]
        public void AdvancePromotesThePreviewBlock()
        {
            GameBoard board = new GameBoard(Lines);
            Block previousNext = board.Next;
            board.Advance();
            Assert.IsSame(previousNext, board.Current, "the preview block became the current one");
        }

        [Test]
        public void AdvanceCreatesAFreshPreviewBlock()
        {
            GameBoard board = new GameBoard(Lines);
            Block previousNext = board.Next;
            board.Advance();
            Assert.IsNotSame(previousNext, board.Next, "a new preview block was created");
            Assert.IsNotNull(board.Next, "preview block is not null");
        }

        [Test]
        public void IsGameOverIsFalseOnAFreshBoard()
        {
            GameBoard board = new GameBoard(Lines);
            Assert.IsFalse(board.IsGameOver(), "a fresh board is playable");
        }

        [Test]
        public void IsGameOverDetectsAFilledTopLine()
        {
            GameBoard board = new GameBoard(Lines);
            for (int j = GameBoard.WallThickness + 1;
                 j < GameBoard.Columns - GameBoard.WallThickness; j++)
                board.Cells[2][j] = GameBoard.Settled;

            Assert.IsTrue(board.IsGameOver(), "a filled top line ends the game");
        }
    }
}
