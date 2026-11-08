using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Round-2 regression probe, adapted to the round-3 class split.
//
//   1. GDI handle count stays flat across restarts (cached objects released)
//   2. the repaint really is partial - proven from the Paint events the panel
//      raises, so the optimisation cannot be "silently still full-screen"
//   3. the dirty rectangle covers both the old and the new block position
//   4. incremental vs full repaint of the same state, compared pixel by pixel
//      on an off-screen canvas that stands for the double buffer
class ProbeRound2
{
    [DllImport("user32.dll")]
    static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
    const uint GR_GDIOBJECTS = 0;

    static owlgame.MainForm form;
    static owlgame.GameController controller;
    static System.Text.StringBuilder sb = new System.Text.StringBuilder();
    static int failures = 0;
    static List<Rectangle> clips = new List<Rectangle>();

    // Shared with FullPaint(): a local function would need C# 7, and csc
    // v4.0.30319 only speaks C# 5.
    static int wRef, hRef;

    static void Log(string s) { sb.AppendLine(s); }
    static void Check(string name, bool ok, string detail)
    {
        Log((ok ? "[PASS] " : "[FAIL] ") + name + "  " + detail);
        if (!ok) failures++;
    }

    static object Get(object target, string name)
    {
        FieldInfo fi = target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return fi == null ? null : fi.GetValue(target);
    }

    static uint GdiCount()
    {
        using (Process p = Process.GetCurrentProcess())
            return GetGuiResources(p.Handle, GR_GDIOBJECTS);
    }

    static Rectangle BlockRect(owlgame.Block block, int cellWidth, int cellHeight)
    {
        int minI = owlgame.Block.MatrixSize, maxI = -1;
        int minJ = owlgame.Block.MatrixSize, maxJ = -1;
        for (int i = 0; i < owlgame.Block.MatrixSize; i++)
            for (int j = 0; j < owlgame.Block.MatrixSize; j++)
                if (block.Matrix[i, j] == owlgame.GameBoard.Settled)
                {
                    if (i < minI) minI = i;
                    if (i > maxI) maxI = i;
                    if (j < minJ) minJ = j;
                    if (j > maxJ) maxJ = j;
                }
        if (maxI < 0) return Rectangle.Empty;
        return Rectangle.FromLTRB((block.Column + minJ) * cellWidth,
                                  (block.Row + minI) * cellHeight,
                                  (block.Column + maxJ + 1) * cellWidth,
                                  (block.Row + maxI + 1) * cellHeight);
    }

    static Bitmap FullPaint()
    {
        Bitmap bmp = new Bitmap(wRef, hRef);
        using (Graphics g = Graphics.FromImage(bmp))
            controller.Draw(new PaintEventArgs(g, new Rectangle(0, 0, wRef, hRef)));
        return bmp;
    }

    static int CountGray(Bitmap bmp)
    {
        int n = 0;
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
            {
                Color c = bmp.GetPixel(x, y);
                if (c.R == 128 && c.G == 128 && c.B == 128) n++;
            }
        return n;
    }

    static int DiffPixels(Bitmap a, Bitmap b, out string where)
    {
        where = "";
        int n = 0;
        for (int y = 0; y < a.Height; y++)
            for (int x = 0; x < a.Width; x++)
            {
                Color ca = a.GetPixel(x, y), cb = b.GetPixel(x, y);
                if (ca.ToArgb() != cb.ToArgb())
                {
                    if (n == 0)
                        where = "first diff (" + x + "," + y + ") incremental="
                              + ca + " full=" + cb;
                    n++;
                }
            }
        return n;
    }

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        form = new owlgame.MainForm();
        form.Show();
        Application.DoEvents();

        Panel view = (Panel)Get(form, "gamewiew");
        Button bs = (Button)Get(form, "buttonstart");
        Button bb = (Button)Get(form, "buttonback");

        view.Paint += delegate(object s, PaintEventArgs e) { clips.Add(e.ClipRectangle); };

        // ---------- 1. GDI handles across restarts ----------
        bs.PerformClick();
        Application.DoEvents();
        view.Update();
        bb.PerformClick();
        Application.DoEvents();

        uint before = GdiCount();
        for (int k = 0; k < 20; k++)
        {
            bb.PerformClick();
            Application.DoEvents();
        }
        Application.DoEvents();
        uint after = GdiCount();
        Log("GDI objects: before=" + before + " after 20 restarts=" + after);
        Check("no GDI leak on restart", after <= before + 2,
              "before=" + before + " after=" + after + " (allow +2 slack)");

        // Freeze the clock so the block cannot drop between measurements.
        Timer clock = (Timer)Get(form, "gametimer");
        bool clockWasEnabled = clock.Enabled;
        clock.Enabled = false;
        Application.DoEvents();

        controller = (owlgame.GameController)Get(form, "controller");
        int cellWidth = controller.CellWidth;
        int cellHeight = controller.CellHeight;
        owlgame.GameBoard board = controller.Board;
        Log("cell size    : cellWidth=" + cellWidth + " cellHeight=" + cellHeight
            + " grid=" + owlgame.GameBoard.Columns + "x" + board.LineCount);

        int w = view.Width, h = view.Height;
        wRef = w; hRef = h;
        Rectangle fullRect = new Rectangle(0, 0, w, h);

        // The canvas stands for the double buffer.
        Bitmap canvas = new Bitmap(w, h);
        using (Graphics g = Graphics.FromImage(canvas))
        {
            g.Clear(Color.Black);
            controller.Draw(new PaintEventArgs(g, fullRect));
        }

        Random rnd = new Random(2026);
        int steps = 0, ghostSteps = 0, worst = 0;
        string worstWhere = "";
        int partialRects = 0, fullRects = 0;
        int uncovered = 0, noPaint = 0;
        long fullArea = (long)w * h;

        for (int k = 0; k < 80; k++)
        {
            Rectangle oldBox = BlockRect(board.Current, cellWidth, cellHeight);

            clips.Clear();
            int op = rnd.Next(3);
            if (op == 0) controller.Move(rnd.Next(2) == 0);
            else if (op == 1) controller.Rotate();
            else controller.Down(1);

            view.Update();
            Application.DoEvents();

            Rectangle newBox = BlockRect(board.Current, cellWidth, cellHeight);

            // ---- 3. geometry: dirty area must cover old and new position ----
            Rectangle mustCover = oldBox.IsEmpty ? newBox : Rectangle.Union(oldBox, newBox);
            if (!mustCover.IsEmpty)
            {
                bool covered = clips.Count == 0;
                foreach (Rectangle r in clips)
                    if (r.Contains(mustCover)) { covered = true; break; }
                if (!covered) uncovered++;
            }

            // ---- 2. classify the dirty rectangles ----
            foreach (Rectangle r in clips)
            {
                long area = (long)r.Width * r.Height;
                if (area <= 0) continue;
                if (area >= fullArea * 95 / 100) fullRects++;
                else partialRects++;
            }

            // ---- 4. replay the real dirty rectangles onto the canvas ----
            if (clips.Count == 0)
            {
                noPaint++;
            }
            else
            {
                foreach (Rectangle r in clips)
                {
                    Rectangle dirty = Rectangle.Intersect(r, fullRect);
                    if (dirty.Width <= 0 || dirty.Height <= 0) continue;
                    using (Graphics g = Graphics.FromImage(canvas))
                    {
                        g.SetClip(dirty);
                        controller.Draw(new PaintEventArgs(g, dirty));
                    }
                }
            }

            using (Bitmap reference = FullPaint())
            {
                string where;
                int d = DiffPixels(canvas, reference, out where);
                steps++;
                if (d > 0)
                {
                    ghostSteps++;
                    if (d > worst) { worst = d; worstWhere = where; }
                }
            }
        }

        clock.Enabled = clockWasEnabled;

        Log("repaint rects: partial=" + partialRects + " full=" + fullRects
            + " (over 80 operations, " + noPaint + " without repaint)");
        Check("repaint is partial, not full-screen", partialRects > fullRects,
              "partial=" + partialRects + " full=" + fullRects);
        Check("dirty rect covers old+new block", uncovered == 0,
              uncovered + " / 80 operations left part of the block outside the dirty rect");
        Log("ghost check  : steps=" + steps + " stepsWithGhost=" + ghostSteps
            + " worstDiffPixels=" + worst);
        if (worstWhere != "") Log("               " + worstWhere);
        Check("incremental repaint == full repaint", ghostSteps == 0,
              ghostSteps + " / " + steps + " steps differ, worst " + worst + " px");

        using (Bitmap reference = FullPaint())
        {
            int gray = CountGray(reference);
            Log("walls replayed: gray=" + gray + " size=" + w + "x" + h);
            Check("walls painted", gray > 10000, "gray=" + gray);
        }
        canvas.Dispose();

        form.Close();
        Log("");
        Log(failures == 0 ? "RESULT: ALL PASS" : "RESULT: " + failures + " FAILURE(S)");
        System.IO.File.WriteAllText("out_round2.txt", sb.ToString(),
            new System.Text.UTF8Encoding(false));
        return failures;
    }
}
