using System;
using System.Drawing;
using System.Windows.Forms;

namespace owlgame
{
    /// <summary>
    /// Draws a <see cref="GameBoard"/> into the panels of the main window.
    ///
    /// This class owns everything that is Windows-specific - the view, the cell
    /// geometry in pixels, the cached GDI objects and the invalidation
    /// rectangle - while the rules live in <see cref="GameBoard"/>.
    /// </summary>
    public class GameController : IDisposable
    {
        /// <summary>Colour of cells that already landed.</summary>
        private static readonly Color SettledColor = Color.AliceBlue;

        /// <summary>Colour of the board walls.</summary>
        private static readonly Color WallColor = Color.Gray;

        private readonly GameBoard board;
        private readonly Panel view;
        private readonly int cellWidth;
        private readonly int cellHeight;

        // Cached GDI objects. Creating a Pen / SolidBrush on every paint (the
        // original code did, then disposed it) is pure overhead: the same
        // handful of objects is reused for the whole game. Released by
        // Dispose().
        private SolidBrush bgBrush;          // view background
        private SolidBrush settledBrush;     // settled cells
        private SolidBrush wallBrush;        // walls
        private SolidBrush currentBrush;     // falling block (colour per shape)
        private SolidBrush nextBrush;        // preview block (colour per shape)
        private SolidBrush previewBgBrush;   // preview pane background
        private Pen cellPen;                 // cell outline

        public GameController(Panel view)
        {
            this.view = view;

            // The board grows downwards: its height follows the aspect ratio of
            // the view, the width is fixed at GameBoard.Columns cells.
            int lineCount = GameBoard.Columns * view.Height / view.Width;
            cellWidth = view.Width / GameBoard.Columns;
            cellHeight = view.Height / lineCount;

            board = new GameBoard(lineCount);

            bgBrush = new SolidBrush(view.BackColor);
            settledBrush = new SolidBrush(SettledColor);
            wallBrush = new SolidBrush(WallColor);
            currentBrush = new SolidBrush(board.Current.BlockColor);
            nextBrush = new SolidBrush(board.Next.BlockColor);
            cellPen = new Pen(view.BackColor);
        }

        /// <summary>Board this controller draws.</summary>
        public GameBoard Board { get { return board; } }

        /// <summary>Cell size in pixels.</summary>
        public int CellWidth { get { return cellWidth; } }
        public int CellHeight { get { return cellHeight; } }

        // ----- rules: delegate to the board, then repaint what moved -----

        /// <summary>Move the falling block down; returns false when it landed.</summary>
        public bool Down(int steps)
        {
            Rectangle before = BlockBounds(board.Current);
            if (!board.MoveDown(steps))
                return false;
            InvalidateBlock(before);
            return true;
        }

        /// <summary>Move the falling block one column left or right.</summary>
        public void Move(bool right)
        {
            Rectangle before = BlockBounds(board.Current);
            if (!board.Move(right))
                return;
            InvalidateBlock(before);
        }

        /// <summary>Rotate the falling block if the rotated cells fit.</summary>
        public void Rotate()
        {
            Rectangle before = BlockBounds(board.Current);
            if (!board.Rotate())
                return;
            InvalidateBlock(before);
        }

        /// <summary>Freeze the falling block onto the board.</summary>
        public void Merge()
        {
            board.Merge();
        }

        /// <summary>Drop filled lines; returns how many were removed.</summary>
        public int ClearLines()
        {
            return board.ClearLines();
        }

        /// <summary>Promote the preview block to the falling block.</summary>
        public void Advance()
        {
            board.Advance();
        }

        /// <summary>True when the stack reached the top.</summary>
        public bool IsGameOver()
        {
            return board.IsGameOver();
        }

        // ----- drawing -----

        /// <summary>
        /// Paint the board. Only the cells that intersect the dirty rectangle
        /// are visited; everything else on screen already holds the right
        /// pixels because the panel is double buffered.
        /// </summary>
        public void Draw(PaintEventArgs e)
        {
            // The view carries the Opaque style flag, so Windows no longer
            // erases the background for us: cover the dirty region first,
            // otherwise whatever was underneath stays visible.
            Rectangle clip = e.ClipRectangle;
            e.Graphics.FillRectangle(bgBrush, clip);

            int firstLine = Math.Max(0, clip.Top / cellHeight);
            int lastLine = Math.Min(board.LineCount - 1, (clip.Bottom - 1) / cellHeight);
            int firstColumn = Math.Max(0, clip.Left / cellWidth);
            int lastColumn = Math.Min(GameBoard.Columns - 1, (clip.Right - 1) / cellWidth);

            for (int i = firstLine; i <= lastLine; ++i)
                for (int j = firstColumn; j <= lastColumn; ++j)
                {
                    if (board.CellAt(i, j) == GameBoard.Settled)
                        PaintCell(e.Graphics, settledBrush, j, i);
                    else if (board.CellAt(i, j) == GameBoard.Wall)
                        PaintCell(e.Graphics, wallBrush, j, i);
                }

            // Falling block: same clipping rule as the cells above.
            Block current = board.Current;
            currentBrush.Color = current.BlockColor;
            for (int i = 0; i < Block.MatrixSize; ++i)
                for (int j = 0; j < Block.MatrixSize; ++j)
                    if (current.Matrix[i, j] == GameBoard.Settled)
                    {
                        int line = i + current.Row;
                        int column = j + current.Column;
                        if (line >= firstLine && line <= lastLine
                            && column >= firstColumn && column <= lastColumn)
                            PaintCell(e.Graphics, currentBrush, column, line);
                    }
        }

        /// <summary>
        /// Paint the "next block" preview.
        ///
        /// proimg is Opaque as well, so the background has to be covered even
        /// when there is nothing to draw - otherwise the pane keeps whatever was
        /// underneath it.
        /// </summary>
        public void DrawPreview(PictureBox box, PaintEventArgs e)
        {
            if (previewBgBrush == null || previewBgBrush.Color != box.BackColor)
            {
                if (previewBgBrush != null)
                    previewBgBrush.Dispose();
                previewBgBrush = new SolidBrush(box.BackColor);
            }
            e.Graphics.FillRectangle(previewBgBrush, e.ClipRectangle);

            Block next = board.Next;
            nextBrush.Color = next.BlockColor;
            for (int i = 0; i < Block.MatrixSize; ++i)
                for (int j = 0; j < Block.MatrixSize; ++j)
                    if (next.Matrix[i, j] == GameBoard.Settled)
                    {
                        // The preview pane is much smaller than the board, so the
                        // block is placed by a fixed fraction of the pane instead
                        // of being centred mathematically.
                        Rectangle cell = new Rectangle(
                            box.Width / PreviewColumnDivisor + j * cellWidth,
                            box.Height / PreviewRowDivisor + i * cellHeight,
                            cellWidth, cellHeight);
                        e.Graphics.FillRectangle(nextBrush, cell);
                        e.Graphics.DrawRectangle(cellPen, cell);
                    }
        }

        /// <summary>Divisor that positions the preview block horizontally.</summary>
        private const int PreviewColumnDivisor = 4;

        /// <summary>Divisor that positions the preview block vertically.</summary>
        private const int PreviewRowDivisor = 9;

        // ----- invalidation -----

        /// <summary>
        /// Repaint only the cells the falling block touched.
        ///
        /// Callers pass the bounding box of the block *before* it moved, so the
        /// union of the old and the new box gets invalidated and no stale pixels
        /// stay on screen. Ghosting is harder to notice than the flicker this
        /// class used to have, which is why tests/Round2Regression.cs compares
        /// an incremental repaint against a full one pixel by pixel.
        ///
        /// The offset must be (column + CurColumn) * cellWidth. An early version
        /// subtracted four whole cells and left half cells behind.
        /// </summary>
        private void InvalidateBlock(Rectangle before)
        {
            Rectangle after = BlockBounds(board.Current);
            Rectangle dirty = before.IsEmpty ? after : Rectangle.Union(before, after);
            if (!dirty.IsEmpty)
                view.Invalidate(dirty);
        }

        /// <summary>
        /// Bounding box of a block's filled cells in client pixels. It doubles as
        /// the invalidation rectangle, so it has to cover every pixel the block
        /// paints - the filled cell plus the border drawn on top of it.
        /// </summary>
        private Rectangle BlockBounds(Block block)
        {
            int minI = Block.MatrixSize, maxI = -1;
            int minJ = Block.MatrixSize, maxJ = -1;
            for (int i = 0; i < Block.MatrixSize; ++i)
                for (int j = 0; j < Block.MatrixSize; ++j)
                    if (block.Matrix[i, j] == GameBoard.Settled)
                    {
                        if (i < minI) minI = i;
                        if (i > maxI) maxI = i;
                        if (j < minJ) minJ = j;
                        if (j > maxJ) maxJ = j;
                    }

            if (maxI < 0)
                return Rectangle.Empty;

            return Rectangle.FromLTRB(
                (block.Column + minJ) * cellWidth,
                (block.Row + minI) * cellHeight,
                (block.Column + maxJ + 1) * cellWidth,
                (block.Row + maxI + 1) * cellHeight);
        }

        /// <summary>Fill one cell, then outline it with cellPen.</summary>
        private void PaintCell(Graphics g, SolidBrush brush, int column, int line)
        {
            Rectangle cell = new Rectangle(column * cellWidth, line * cellHeight,
                                           cellWidth, cellHeight);
            g.FillRectangle(brush, cell);
            g.DrawRectangle(cellPen, cell);
        }

        /// <summary>Release the cached GDI objects.</summary>
        public void Dispose()
        {
            if (cellPen != null) { cellPen.Dispose(); cellPen = null; }
            if (currentBrush != null) { currentBrush.Dispose(); currentBrush = null; }
            if (nextBrush != null) { nextBrush.Dispose(); nextBrush = null; }
            if (wallBrush != null) { wallBrush.Dispose(); wallBrush = null; }
            if (settledBrush != null) { settledBrush.Dispose(); settledBrush = null; }
            if (bgBrush != null) { bgBrush.Dispose(); bgBrush = null; }
            if (previewBgBrush != null) { previewBgBrush.Dispose(); previewBgBrush = null; }
        }
    }
}
