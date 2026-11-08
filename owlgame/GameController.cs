using System;
using System.Collections.Generic;
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
        private static readonly Color SettledColor = Color.FromArgb(206, 214, 232);

        /// <summary>
        /// Colour of the board walls. Deliberately close to the background: the
        /// walls are structure, not content, and a mid grey grid competed with
        /// the falling blocks for attention.
        /// </summary>
        private static readonly Color WallColor = Color.FromArgb(44, 44, 62);

        /// <summary>Colour flashed over the lines that are about to vanish.</summary>
        private static readonly Color HighlightColor = Color.FromArgb(255, 236, 150);

        /// <summary>
        /// Pixels trimmed from each side of a cell. The gap between cells reads as
        /// a grid and replaces the old 1px outline, which made every cell look
        /// like a button.
        /// </summary>
        private const int CellGap = 1;

        /// <summary>Top highlight that gives each cell a slight lift.</summary>
        private static readonly Color CellSheen = Color.FromArgb(56, 255, 255, 255);

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
        private SolidBrush highlightBrush;   // flashing lines
        private SolidBrush sheenBrush;       // per cell top highlight


        // Lines waiting for the clear animation to finish. Null means "no
        // animation in progress".
        private List<int> highlightedLines;

        // Opacity of the clear flash, faded a step at a time by AdvanceFlash().
        private int flashAlpha;

        /// <summary>Opacity the flash starts at.</summary>
        private const int FlashAlphaStart = 235;

        /// <summary>How much opacity each step removes.</summary>
        private const int FlashAlphaStep = 70;

        /// <summary>Opacity the flash never goes below.</summary>
        private const int FlashAlphaFloor = 25;

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
            highlightBrush = new SolidBrush(HighlightColor);
            sheenBrush = new SolidBrush(CellSheen);

        }

        /// <summary>Board this controller draws.</summary>
        public GameBoard Board { get { return board; } }

        // The palette is exposed so the pixel tests can derive their expectations
        // from the product instead of hard coding colour literals. Restyling the
        // board then updates the tests automatically - the previous fixed numbers
        // (17200 grey pixels, 128/128/128) all broke the moment the colours
        // changed, which told us nothing about whether the board was still drawn
        // correctly.
        /// <summary>Colour of settled cells.</summary>
        public static Color SettledColour { get { return SettledColor; } }

        /// <summary>Colour of the walls.</summary>
        public static Color WallColour { get { return WallColor; } }

        /// <summary>Colour of the lines that are being cleared.</summary>
        public static Color HighlightColour { get { return HighlightColor; } }

        /// <summary>Pixels trimmed from each side of a block cell (walls have no gap).</summary>
        public const int CellGapPixels = CellGap;

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

        /// <summary>Indices of the lines that are currently complete.</summary>
        public List<int> FindFullLines()
        {
            return board.FindFullLines();
        }

        /// <summary>
        /// Flash the given lines before they are removed. Purely visual: the
        /// board is not touched until <see cref="ClearLines(IList{int})"/> runs.
        /// </summary>
        public void HighlightLines(List<int> linesToHighlight)
        {
            highlightedLines = linesToHighlight;
            flashAlpha = FlashAlphaStart;
            highlightBrush.Color = Color.FromArgb(flashAlpha, HighlightColor);
        }

        /// <summary>
        /// Fade the clear flash one step. Called by the flash timer a few times
        /// before the lines are actually removed, so the row fades out instead of
        /// blinking off.
        /// </summary>
        public void AdvanceFlash()
        {
            flashAlpha -= FlashAlphaStep;
            if (flashAlpha < FlashAlphaFloor)
                flashAlpha = FlashAlphaFloor;
            highlightBrush.Color = Color.FromArgb(flashAlpha, HighlightColor);
        }

        /// <summary>True while a clear animation is showing.</summary>
        public bool IsHighlighting
        {
            get { return highlightedLines != null && highlightedLines.Count > 0; }
        }

        /// <summary>Remove the given lines and drop the lines above them.</summary>
        public int ClearLines(IList<int> linesToClear)
        {
            highlightedLines = null;
            return board.ClearLines(linesToClear);
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
                    // Walls are filled edge to edge so they read as one solid
                    // frame; only the blocks get the gap that makes them stand out.
                    if (board.CellAt(i, j) == GameBoard.Settled)
                        PaintCell(e.Graphics, settledBrush, j, i, CellGap);
                    else if (board.CellAt(i, j) == GameBoard.Wall)
                        PaintCell(e.Graphics, wallBrush, j, i, 0);
                }

            // Lines waiting for the clear animation: painted over the cells so
            // they read as "these are about to disappear".
            if (highlightedLines != null)
                foreach (int line in highlightedLines)
                {
                    if (line < firstLine || line > lastLine)
                        continue;
                    Rectangle band = new Rectangle(0, line * cellHeight,
                                                   view.Width, cellHeight);
                    e.Graphics.FillRectangle(highlightBrush, band);
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
                            PaintCell(e.Graphics, currentBrush, column, line, CellGap);
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
                        // The whole 5x5 matrix is centred in the pane, so the
                        // block sits in the middle no matter which shape it is.
                        // (It used to be placed by a hard coded 1/4 and 1/9 of the
                        // pane, which drifted once the pane was resized.)
                        Rectangle cell = new Rectangle(
                            (box.Width - Block.MatrixSize * cellWidth) / 2 + j * cellWidth + CellGap,
                            (box.Height - Block.MatrixSize * cellHeight) / 2 + i * cellHeight + CellGap,
                            cellWidth - CellGap * 2,
                            cellHeight - CellGap * 2);
                        e.Graphics.FillRectangle(nextBrush, cell);
                        if (cell.Height > 4)
                            e.Graphics.FillRectangle(sheenBrush, cell.X, cell.Y, cell.Width, 1);
                    }
        }

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

        /// <summary>
        /// Fill one cell with a small gap around it and a one pixel sheen on top.
        ///
        /// The gap replaces the old outline: an outlined cell reads as a raised
        /// button, a cell separated by a gap reads as part of a grid. The cell
        /// still occupies its full cellWidth x cellHeight slot, so the invalidation
        /// rectangle and the cell geometry stay exactly as they were.
        /// </summary>
        private void PaintCell(Graphics g, SolidBrush brush, int column, int line, int gap)
        {
            Rectangle cell = new Rectangle(
                column * cellWidth + gap,
                line * cellHeight + gap,
                cellWidth - gap * 2,
                cellHeight - gap * 2);

            g.FillRectangle(brush, cell);

            if (gap > 0 && cell.Height > 4)
                g.FillRectangle(sheenBrush, cell.X, cell.Y, cell.Width, 1);
        }

        /// <summary>Release the cached GDI objects.</summary>
        public void Dispose()
        {
            if (sheenBrush != null) { sheenBrush.Dispose(); sheenBrush = null; }
            if (currentBrush != null) { currentBrush.Dispose(); currentBrush = null; }
            if (nextBrush != null) { nextBrush.Dispose(); nextBrush = null; }
            if (wallBrush != null) { wallBrush.Dispose(); wallBrush = null; }
            if (settledBrush != null) { settledBrush.Dispose(); settledBrush = null; }
            if (bgBrush != null) { bgBrush.Dispose(); bgBrush = null; }
            if (highlightBrush != null) { highlightBrush.Dispose(); highlightBrush = null; }
            if (previewBgBrush != null) { previewBgBrush.Dispose(); previewBgBrush = null; }
        }
    }
}
