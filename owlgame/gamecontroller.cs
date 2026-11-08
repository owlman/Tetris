using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace owlgame
{
    class gameController : IDisposable
    {
        // game interface:
        public gameController(Panel v) 
        {
            view = v;
            lineNo = rowNo * v.Height / v.Width;
            bw = v.Width / rowNo;
            bh = v.Height / lineNo;
            map = new List<int[]>();
            now = new block(rowNo / 2 - 2, 0);            
            for (int i = 0; i < lineNo; ++i)
                map.Add(new int[rowNo]);  
            initMap();
            wait = new block(rowNo / 2 - 2, 0);

            // Cached GDI objects. Creating a Pen / SolidBrush on every paint
            // (the old code did, then disposed it) is pure overhead: the same
            // handful of objects is reused for the whole game. Released by
            // Dispose(), see the bottom of the class.
            bgBrush = new SolidBrush(view.BackColor);
            blockBrush = new SolidBrush(Color.AliceBlue);
            wallBrush = new SolidBrush(Color.Gray);
            nowBrush = new SolidBrush(now.colorblock);
            waitBrush = new SolidBrush(wait.colorblock);
            cellPen = new Pen(view.BackColor);
        }
        public void initMap()
        {            
            for (int i = 0; i < lineNo; ++i)
                for (int j = 0; j < rowNo; ++j)
                {
                    if (j < 2 || j > rowNo - 3 || i > lineNo - 3)
                         map[i][j] = 2;
                }
        }      

       public bool down(int i)
        {
            if (i < 1)
                i = 1;
            int newy = now.cury + 1*i;
            if (test(now.curx, newy, now.matblock))
            {
                Rectangle before = BlockBounds(now);
                now.cury = newy;
                InvalidateBlock(before);
                return true;
            }
            return false; 
        }
        public void move(bool sign) 
        {
            int newx;
            if (sign)
                newx = now.curx + 1;
            else 
                newx = now.curx - 1;
            if (newx >= 0 && newx+block.size <= rowNo
                && test(newx,now.cury,now.matblock))
            {
                Rectangle before = BlockBounds(now);
                now.curx = newx;
                InvalidateBlock(before);
            }
        }
        public void transpose()
        {
            // Copy constructor: keeps shape / colour while leaving the random
            // source untouched (see block.block(block)).
            block temp = new block(now);
            for (int i = 0; i < block.size; ++i)
                for (int j = block.size-1; j >= 0; --j)
                    temp.matblock[j, i] =  now.matblock[i, block.size-1 - j];
            if (temp.curx >=0 && temp.curx+block.size <= rowNo
                && test(temp.curx, temp.cury, temp.matblock))
            {
                Rectangle before = BlockBounds(now);
                now = temp;
                InvalidateBlock(before);
            }
        }
        /// <summary>
        /// Repaint only the cells the falling block touched.
        ///
        /// An earlier version invalidated the whole panel on every key press.
        /// That is correct but wasteful: the board is 20 x 38 cells and only a
        /// handful change. The original partial version was dropped because it
        /// applied the wrong offset - (j + now.curx - 4) * bw shifted the
        /// rectangle by four whole cells, leaving half cells behind. The
        /// offset must be (j + now.curx) * bw.
        ///
        /// Callers pass the bounding box of the block *before* it moved, so the
        /// union of the old and new boxes is repainted and no stale pixels stay
        /// on screen (that would be ghosting, which is harder to spot than the
        /// flicker this class used to have).
        /// </summary>
        private void InvalidateBlock(Rectangle before)
        {
            Rectangle after = BlockBounds(now);
            Rectangle dirty = before.IsEmpty ? after : Rectangle.Union(before, after);
            if (!dirty.IsEmpty)
                view.Invalidate(dirty);
        }

        /// <summary>
        /// Bounding box of the block's filled cells in client pixels.
        /// It doubles as the invalidation rectangle, so it has to cover every
        /// pixel the block paints - the filled cell plus the border drawn on
        /// top of it.
        /// </summary>
        private Rectangle BlockBounds(block b)
        {
            return BlockBounds(b.curx, b.cury, b.matblock);
        }

        private Rectangle BlockBounds(int curx, int cury, int[,] mat)
        {
            int minI = block.size, maxI = -1, minJ = block.size, maxJ = -1;
            for (int i = 0; i < block.size; ++i)
                for (int j = 0; j < block.size; ++j)
                    if (mat[i, j] == 1)
                    {
                        if (i < minI) minI = i;
                        if (i > maxI) maxI = i;
                        if (j < minJ) minJ = j;
                        if (j > maxJ) maxJ = j;
                    }

            if (maxI < 0)
                return Rectangle.Empty;

            return Rectangle.FromLTRB(
                (curx + minJ) * bw,
                (cury + minI) * bh,
                (curx + maxJ + 1) * bw,
                (cury + maxI + 1) * bh);
        }

        /// <summary>Fill one cell, then outline it with cellPen.</summary>
        private void PaintCell(Graphics g, SolidBrush brush, int col, int row)
        {
            Rectangle r = new Rectangle(col * bw, row * bh, bw, bh);
            g.FillRectangle(brush, r);
            g.DrawRectangle(cellPen, r);
        }

        /// <summary>Release the cached GDI objects.</summary>
        public void Dispose()
        {
            if (cellPen != null) { cellPen.Dispose(); cellPen = null; }
            if (nowBrush != null) { nowBrush.Dispose(); nowBrush = null; }
            if (waitBrush != null) { waitBrush.Dispose(); waitBrush = null; }
            if (wallBrush != null) { wallBrush.Dispose(); wallBrush = null; }
            if (blockBrush != null) { blockBrush.Dispose(); blockBrush = null; }
            if (bgBrush != null) { bgBrush.Dispose(); bgBrush = null; }
            if (previewBgBrush != null) { previewBgBrush.Dispose(); previewBgBrush = null; }
        }
        public bool test(int mi,int mj,int[,]m) 
        {
            for (int j = 0; j < block.size; ++j)
                for (int i = 0; i < block.size; ++i)
                   if (map[mj+j][mi+i]>=1 && m[j,i]==1)
                       return false;
            return true;
        }
        public void draw(PaintEventArgs e)
        {
            // The view carries the Opaque style flag, so Windows no longer
            // erases the background for us: cover the dirty region first,
            // otherwise whatever was underneath stays visible.
            Rectangle clip = e.ClipRectangle;
            e.Graphics.FillRectangle(bgBrush, clip);

            // Walk only the cells that intersect the dirty rectangle. A cell is
            // bw x bh pixels; bh * lineNo can be a few pixels short of the
            // panel height, and that leftover strip is covered by the
            // background fill above.
            int i0 = Math.Max(0, clip.Top / bh);
            int i1 = Math.Min(lineNo - 1, (clip.Bottom - 1) / bh);
            int j0 = Math.Max(0, clip.Left / bw);
            int j1 = Math.Min(rowNo - 1, (clip.Right - 1) / bw);

            for (int i = i0; i <= i1; ++i)
                for (int j = j0; j <= j1; ++j)
                {
                    if (map[i][j] == 1)
                        PaintCell(e.Graphics, blockBrush, j, i);
                    else if (map[i][j] == 2)
                        PaintCell(e.Graphics, wallBrush, j, i);
                }

            // Falling block: same clipping rule as the cells above.
            nowBrush.Color = now.colorblock;
            for (int i = 0; i < block.size; ++i)
                for (int j = 0; j < block.size; ++j)
                    if (now.matblock[i, j] == 1)
                    {
                        int ci = i + now.cury;
                        int cj = j + now.curx;
                        if (ci >= i0 && ci <= i1 && cj >= j0 && cj <= j1)
                            PaintCell(e.Graphics, nowBrush, cj, ci);
                    }
        }
        public void putblock()
        {                         ;
        for (int i = 0; i < block.size; ++i)
                for (int j = 0; j < block.size; ++j) 
                    if (now.matblock[i,j] == 1)
                        map[i+now.cury][j+now.curx] = now.matblock[i,j];
        }
        public void clearLine() 
        {
            bool isclear = false;
            for (int i = 0; i < lineNo - 2; ++i)
            {
                isclear = true;
                for (int j = 2; j < rowNo - 2; ++j)
                    if (map[i][j] != 1)
                    {
                        isclear = false;
                        break;
                    }
                if (isclear)
                {
                    map.RemoveAt(i);
                    map.Insert(0, new int[rowNo]);
                    initMap();
                }
            }
        }
        public void startnext()
        {
            now = wait;
            wait = new block(rowNo / 2 - 2, 0);
        }
        public void drawwait(PictureBox box, PaintEventArgs e)
        {
            // Same contract as draw(): proimg is Opaque as well, so the
            // background has to be covered before the preview is painted.
            if (previewBgBrush == null || previewBgBrush.Color != box.BackColor)
            {
                if (previewBgBrush != null)
                    previewBgBrush.Dispose();
                previewBgBrush = new SolidBrush(box.BackColor);
            }
            e.Graphics.FillRectangle(previewBgBrush, e.ClipRectangle);

            waitBrush.Color = wait.colorblock;
            for (int i = 0; i < block.size; ++i)
                for (int j = 0; j < block.size; ++j)
                {
                    if (wait.matblock[i, j] == 1)
                    {
                        Rectangle rect = new Rectangle
                        (
                            box.Width/4 + j*bw,
                            box.Height/9 + i*bh,
                            bw, bh
                        );
                        e.Graphics.FillRectangle(waitBrush, rect);
                        e.Graphics.DrawRectangle(cellPen, rect);
                    }
                }
        }
        public bool fall()
        {
            for (int i = 3; i < rowNo-2; ++i)
            {
                if (map[2][i] == 1)
                    return true;
            }
            return false;
        }

        // game data:
        private block now;
        private block wait;
        private List<int[]> map;
        private Panel view;
        private int bw;
        private int bh;
        private const int rowNo = 20;
        private int lineNo;

        // Cached GDI objects: created once in the constructor, released in
        // Dispose(). See draw() / drawwait().
        private SolidBrush bgBrush;          // view background
        private SolidBrush blockBrush;       // settled cells
        private SolidBrush wallBrush;        // walls
        private SolidBrush nowBrush;         // falling block (colour per shape)
        private SolidBrush waitBrush;        // preview block (colour per shape)
        private SolidBrush previewBgBrush;   // preview pane background
        private Pen cellPen;                 // cell outline
            
     }
}