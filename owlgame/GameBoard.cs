using System;
using System.Collections.Generic;

namespace owlgame
{
    /// <summary>
    /// Board state and game rules.
    ///
    /// This class deliberately does not reference System.Windows.Forms and does
    /// no drawing at all: it only knows cells, the falling block, the preview
    /// block and the rules for moving / merging / clearing lines. That makes the
    /// rules testable without a message loop - previously they were welded to a
    /// Panel and the only way to check them was a hand written probe.
    ///
    /// The drawing counterpart is <see cref="gameController"/>.
    /// </summary>
    public class GameBoard
    {
        /// <summary>Board width in cells. Fixed; the height follows the view aspect.</summary>
        public const int Columns = 20;

        /// <summary>Thickness of the left and right walls, in cells.</summary>
        public const int WallThickness = 2;

        /// <summary>Thickness of the floor, in cells.</summary>
        public const int FloorThickness = 2;

        /// <summary>Cell values used on the board.</summary>
        public const int Empty = 0;
        public const int Settled = 1;
        public const int Wall = 2;

        /// <summary>Lines that have to be cleared to reach the next level.</summary>
        public const int LinesPerLevel = 10;

        /// <summary>
        /// Points for clearing 1, 2, 3 or 4 lines at once, before the level
        /// multiplier. The classic table rewards clearing several lines with one
        /// block far more than clearing them one by one.
        /// </summary>
        private static readonly int[] LineScores = { 0, 100, 300, 500, 800 };

        /// <summary>Drop interval of level 1, in milliseconds.</summary>
        public const int BaseDropInterval = 1000;

        /// <summary>Fastest drop interval, in milliseconds.</summary>
        public const int MinDropInterval = 120;

        /// <summary>How much faster each level drops, in milliseconds.</summary>
        public const int DropIntervalStep = 90;

        private readonly int lineCount;
        private readonly List<int[]> cells;
        private Block current;
        private Block next;
        private int score;
        private int level;
        private int lines;

        public GameBoard(int lineCount)
        {
            if (lineCount < Block.MatrixSize * 2)
                throw new ArgumentOutOfRangeException("lineCount",
                    "the board must be at least " + (Block.MatrixSize * 2) + " lines tall");

            this.lineCount = lineCount;
            cells = new List<int[]>();
            for (int i = 0; i < lineCount; ++i)
                cells.Add(new int[Columns]);

            ResetWalls();
            current = SpawnBlock();
            next = SpawnBlock();
            level = 1;
        }

        /// <summary>Number of board lines (rows).</summary>
        public int LineCount { get { return lineCount; } }

        /// <summary>Board cells; index by [line][column].</summary>
        public List<int[]> Cells { get { return cells; } }

        /// <summary>The falling block.</summary>
        public Block Current { get { return current; } }

        /// <summary>The preview block shown next to the board.</summary>
        public Block Next { get { return next; } }

        /// <summary>Points scored so far.</summary>
        public int Score { get { return score; } }

        /// <summary>Current level, starting at 1.</summary>
        public int Level { get { return level; } }

        /// <summary>Total number of lines cleared.</summary>
        public int Lines { get { return lines; } }

        /// <summary>
        /// Current drop interval in milliseconds. Level 1 drops once a second,
        /// every level is a little faster, and it never goes below
        /// <see cref="MinDropInterval"/>.
        /// </summary>
        public int DropInterval
        {
            get
            {
                int interval = BaseDropInterval - (level - 1) * DropIntervalStep;
                return interval < MinDropInterval ? MinDropInterval : interval;
            }
        }

        /// <summary>Value of one cell: 0 empty, 1 settled, 2 wall.</summary>
        public int CellAt(int line, int column)
        {
            return cells[line][column];
        }

        /// <summary>
        /// Fill the border of the playfield with wall cells. Safe to call
        /// repeatedly: lines pushed in by <see cref="ClearLines"/> are plain
        /// rows and need their wall cells back.
        /// </summary>
        public void ResetWalls()
        {
            for (int i = 0; i < lineCount; ++i)
            {
                int[] line = cells[i];
                for (int j = 0; j < Columns; ++j)
                    if (IsWallColumn(j) || IsFloorLine(i))
                        line[j] = Wall;
            }
        }

        /// <summary>True for the two wall columns on each side.</summary>
        private static bool IsWallColumn(int column)
        {
            return column < WallThickness || column > Columns - 1 - WallThickness;
        }

        /// <summary>True for the floor lines at the bottom of the board.</summary>
        private bool IsFloorLine(int line)
        {
            return line > lineCount - 1 - FloorThickness;
        }

        /// <summary>
        /// A fresh row carrying its wall cells. Pushing a line in after a clear
        /// only needs this row filled in - re-scanning the whole board would be
        /// pointless because every other row already has its walls.
        /// </summary>
        private int[] NewWallLine()
        {
            int[] line = new int[Columns];
            for (int j = 0; j < Columns; ++j)
                if (IsWallColumn(j))
                    line[j] = Wall;
            return line;
        }

        /// <summary>
        /// True when the block matrix placed at (column, row) overlaps nothing
        /// but empty cells.
        ///
        /// The explicit bounds check matters: the original code indexed the
        /// board directly and only survived because the floor happened to be
        /// reached first. Asking for a position below the floor would have
        /// thrown IndexOutOfRangeException.
        /// </summary>
        public bool CanPlace(int column, int row, int[,] matrix)
        {
            if (column < 0 || column + Block.MatrixSize > Columns)
                return false;
            if (row < 0 || row + Block.MatrixSize > lineCount)
                return false;

            for (int i = 0; i < Block.MatrixSize; ++i)
                for (int j = 0; j < Block.MatrixSize; ++j)
                    if (matrix[i, j] == Settled && cells[row + i][column + j] != Empty)
                        return false;

            return true;
        }

        /// <summary>Move the falling block down by <paramref name="steps"/> lines.</summary>
        public bool MoveDown(int steps)
        {
            if (steps < 1)
                steps = 1;

            int newRow = current.Row + steps;
            if (!CanPlace(current.Column, newRow, current.Matrix))
                return false;

            current.Row = newRow;
            return true;
        }

        /// <summary>Move the falling block one column left or right.</summary>
        public bool Move(bool right)
        {
            int newColumn = right ? current.Column + 1 : current.Column - 1;
            if (!CanPlace(newColumn, current.Row, current.Matrix))
                return false;

            current.Column = newColumn;
            return true;
        }

        /// <summary>Rotate the falling block in place if the new cells fit.</summary>
        public bool Rotate()
        {
            Block rotated = current.Rotated();
            if (!CanPlace(rotated.Column, rotated.Row, rotated.Matrix))
                return false;

            current = rotated;
            return true;
        }

        /// <summary>Freeze the falling block into the board cells.</summary>
        public void Merge()
        {
            for (int i = 0; i < Block.MatrixSize; ++i)
                for (int j = 0; j < Block.MatrixSize; ++j)
                    if (current.Matrix[i, j] == Settled)
                        cells[current.Row + i][current.Column + j] = Settled;
        }

        /// <summary>
        /// Indices of the completely filled lines, top to bottom.
        ///
        /// Split out from <see cref="ClearLines()"/> so the caller can show a
        /// short flash on those lines before they disappear; the lines are not
        /// touched here.
        ///
        /// A fully filled line only needs its playfield part checked: the wall
        /// columns are never part of what the player fills.
        /// </summary>
        public List<int> FindFullLines()
        {
            List<int> full = new List<int>();
            for (int i = 0; i < lineCount - FloorThickness; ++i)
            {
                bool complete = true;
                for (int j = WallThickness; j < Columns - WallThickness; ++j)
                    if (cells[i][j] != Settled)
                    {
                        complete = false;
                        break;
                    }

                if (complete)
                    full.Add(i);
            }
            return full;
        }

        /// <summary>
        /// Remove every completely filled line and push the lines above down.
        /// Returns how many lines were removed, and awards the points for them.
        /// </summary>
        public int ClearLines()
        {
            return ClearLines(FindFullLines());
        }

        /// <summary>
        /// Remove the given lines and push everything above them down. The lines
        /// are removed bottom up so the indices stay valid while the list
        /// shrinks.
        /// </summary>
        public int ClearLines(IList<int> linesToClear)
        {
            if (linesToClear == null || linesToClear.Count == 0)
                return 0;

            List<int> sorted = new List<int>(linesToClear);
            sorted.Sort();
            for (int k = sorted.Count - 1; k >= 0; --k)
            {
                int line = sorted[k];
                if (line < 0 || line >= lineCount - FloorThickness)
                    continue;

                cells.RemoveAt(line);
                cells.Insert(0, NewWallLine());
            }

            AwardScore(sorted.Count);
            return sorted.Count;
        }

        /// <summary>
        /// Add the points for a clear, then update the level. The points use the
        /// level the clear happened at, so the level is raised afterwards.
        /// </summary>
        private void AwardScore(int cleared)
        {
            if (cleared <= 0)
                return;

            int baseScore = cleared < LineScores.Length
                ? LineScores[cleared]
                : LineScores[LineScores.Length - 1];
            score += baseScore * level;

            lines += cleared;
            level = lines / LinesPerLevel + 1;
        }

        /// <summary>Promote the preview block and spawn a new one.</summary>
        public void Advance()
        {
            current = next;
            next = SpawnBlock();
        }

        /// <summary>
        /// True when the stack reached the top: the game is over.
        /// The original checked a fixed line (index 2), which is the row right
        /// below the spawn area.
        /// </summary>
        public bool IsGameOver()
        {
            const int topCheckLine = 2;
            for (int j = WallThickness + 1; j < Columns - WallThickness; ++j)
                if (cells[topCheckLine][j] == Settled)
                    return true;
            return false;
        }

        /// <summary>
        /// Spawn position: horizontally centred, top row. For a 20 column board
        /// this is column 8, which is what the original "rowNo / 2 - 2" meant.
        /// </summary>
        private Block SpawnBlock()
        {
            return new Block(Columns / 2 - Block.MatrixSize / 2, 0);
        }
    }
}
