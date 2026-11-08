using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Round-2 regression probe (fully deterministic, no screen capture needed).
//
//   1. GDI handle count stays flat across restarts (cached objects released)
//   2. the repaint really is partial - proven from the Paint events the panel
//      raises, so the optimisation cannot be "silently still full-screen"
//   3. the dirty rectangle covers both the old and the new block position,
//      i.e. no ghost can be left behind (pure geometry)
//   4. incremental vs full repaint of the same state, compared pixel by pixel
//      on an off-screen canvas that stands for the double buffer
//
// Why an off-screen canvas for (4): with OptimizedDoubleBuffer the panel paints
// into a background buffer and only the invalidated rectangle is blitted to the
// screen. Reproducing that is exactly "paint into a bitmap, clipped to the
// dirty rectangle, reusing whatever the bitmap already holds". If the dirty
// rectangle is too small, stale pixels stay in the canvas - the same ghost the
// user would see. Doing it in memory keeps the check deterministic: a screen
// capture depends on window focus, DWM compositing timing and occlusion, all of
// which produce false differences in a headless / busy desktop.
class ProbeRound2
{
    [DllImport("user32.dll")]
    static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
    const uint GR_GDIOBJECTS = 0;

    static owlgame.mainForm form;
    static System.Text.StringBuilder sb = new System.Text.StringBuilder();
    static int failures = 0;
    static List<Rectangle> clips = new List<Rectangle>();

    // Shared with FullPaint(): a local function would need C# 7, and csc
    // v4.0.30319 only speaks C# 5.
    static object gcRef;
    static MethodInfo drawRef;
    static int wRef, hRef;

    static Bitmap FullPaint()
    {
        Bitmap bmp = new Bitmap(wRef, hRef);
        using (Graphics g = Graphics.FromImage(bmp))
            drawRef.Invoke(gcRef, new object[] {
                new PaintEventArgs(g, new Rectangle(0, 0, wRef, hRef)) });
        return bmp;
    }

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

    // Geometry of the block's pixels, from the same fields the controller uses.
    static Rectangle BlockRect(object blk, int bw, int bh)
    {
        int curx = (int)Get(blk, "curx");
        int cury = (int)Get(blk, "cury");
        int[,] m = (int[,])Get(blk, "matblock");
        int minI = 5, maxI = -1, minJ = 5, maxJ = -1;
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                if (m[i, j] == 1)
                {
                    if (i < minI) minI = i;
                    if (i > maxI) maxI = i;
                    if (j < minJ) minJ = j;
                    if (j > maxJ) maxJ = j;
                }
        if (maxI < 0) return Rectangle.Empty;
        return Rectangle.FromLTRB((curx + minJ) * bw, (cury + minI) * bh,
                                  (curx + maxJ + 1) * bw, (cury + maxI + 1) * bh);
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
        form = new owlgame.mainForm();
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

        object gc = Get(form, "controller");
        MethodInfo move = gc.GetType().GetMethod("move");
        MethodInfo transpose = gc.GetType().GetMethod("transpose");
        MethodInfo down = gc.GetType().GetMethod("down");
        MethodInfo draw = gc.GetType().GetMethod("draw");
        int bw = (int)Get(gc, "bw");
        int bh = (int)Get(gc, "bh");
        int rows = (int)Get(gc, "lineNo");
        int cols = (int)gc.GetType().GetField("rowNo",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).GetValue(null);
        Log("cell size    : bw=" + bw + " bh=" + bh + " grid=" + cols + "x" + rows);

        int w = view.Width, h = view.Height;
        Rectangle fullRect = new Rectangle(0, 0, w, h);

        // The canvas stands for the double buffer.
        Bitmap canvas = new Bitmap(w, h);
        using (Graphics g = Graphics.FromImage(canvas))
        {
            g.Clear(Color.Black);
            draw.Invoke(gc, new object[] { new PaintEventArgs(g, fullRect) });
        }

        drawRef = draw;
        gcRef = gc;
        wRef = w;
        hRef = h;

        Random rnd = new Random(2026);
        int steps = 0, ghostSteps = 0, worst = 0;
        string worstWhere = "";
        int partialRects = 0, fullRects = 0;
        int uncovered = 0, noPaint = 0;
        long fullArea = (long)w * h;

        for (int k = 0; k < 80; k++)
        {
            Rectangle oldBox = BlockRect(Get(gc, "now"), bw, bh);

            clips.Clear();
            int op = rnd.Next(3);
            if (op == 0) move.Invoke(gc, new object[] { rnd.Next(2) == 0 });
            else if (op == 1) transpose.Invoke(gc, null);
            else down.Invoke(gc, new object[] { 1 });

            view.Update();
            Application.DoEvents();

            Rectangle newBox = BlockRect(Get(gc, "now"), bw, bh);

            // ---- 3. geometry: dirty area must cover old and new position ----
            Rectangle mustCover = oldBox.IsEmpty ? newBox : Rectangle.Union(oldBox, newBox);
            if (!mustCover.IsEmpty)
            {
                bool covered = clips.Count == 0;    // nothing moved, nothing painted
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
                noPaint++;      // no repaint requested: canvas should already match
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
                        draw.Invoke(gc, new object[] { new PaintEventArgs(g, dirty) });
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

        // ---------- walls / block still rendered ----------
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
