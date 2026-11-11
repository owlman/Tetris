using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

// Renders the real application window into PNG files for the README.
//
// Everything is driven through the real code path: the probe clicks the same
// "start" button a user would, then calls gametimer_Tick (the method the game
// timer calls) to advance the game. Nothing about the UI is mocked - the
// pictures are exactly what the program draws, captured with DrawToBitmap.
//
// Getting lines cleared needs a plan. Two simpler heuristics were tried and
// thrown away:
//   * steering on every tick made the block slide sideways while falling;
//   * steering once per block towards the lowest column built a staircase that
//     never completed a row.
// So the third attempt is a small placement evaluator: for each rotation and
// each starting column it simulates the landing (via the product's own
// GameBoard.CanPlace) and scores the result with the classic
// aggregate-height / lines / holes / bumpiness formula.
//
// Usage:  Capture.exe [output-directory]
//
// Notes:
//   * Requires an interactive desktop session (it calls Form.Show()).
//   * Written for the C# 5 compiler shipped with .NET Framework 4.0, so no
//     string interpolation, no null-conditional operator, no local functions.
class CaptureScreenshots
{
    const BindingFlags Any =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static object Field(object target, string name)
    {
        FieldInfo fi = target.GetType().GetField(name, Any);
        if (fi == null) throw new InvalidOperationException("field not found: " + name);
        return fi.GetValue(target);
    }

    static object Prop(object target, string name)
    {
        PropertyInfo pi = target.GetType().GetProperty(name, Any);
        if (pi == null) throw new InvalidOperationException("property not found: " + name);
        return pi.GetValue(target, null);
    }

    static object Call1(object target, string name)
    {
        MethodInfo mi = target.GetType().GetMethod(name, Any);
        if (mi == null) throw new InvalidOperationException("method not found: " + name);
        return mi.Invoke(target, null);
    }

    static void Call(object target, string name, params object[] args)
    {
        MethodInfo mi = target.GetType().GetMethod(name, Any);
        if (mi == null) throw new InvalidOperationException("method not found: " + name);
        mi.Invoke(target, args);
    }

    static object CallRet(object target, string name, params object[] args)
    {
        MethodInfo mi = target.GetType().GetMethod(name, Any);
        if (mi == null) throw new InvalidOperationException("method not found: " + name);
        return mi.Invoke(target, args);
    }

    // One frame of the game clock, exactly as gametimer_Tick does it.
    static void Tick(Form form)
    {
        Call(form, "gametimer_Tick", null, EventArgs.Empty);
        Application.DoEvents();
    }

    // The clear animation runs on its own timer; let it finish before moving on.
    static void WaitForClear(object controller)
    {
        int guard = 0;
        while ((bool)Prop(controller, "IsHighlighting") && guard++ < 400)
        {
            Thread.Sleep(5);
            Application.DoEvents();
        }
    }

    static void Save(Form form, string path)
    {
        int w = form.ClientSize.Width;
        int h = form.ClientSize.Height;
        using (Bitmap bmp = new Bitmap(w, h))
        {
            form.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
            bmp.Save(path, ImageFormat.Png);
        }
        Console.WriteLine("  wrote " + Path.GetFileName(path) + "  (" + w + "x" + h + ")");
    }

    static int Rows(object board)
    {
        return ((List<int[]>)Prop(board, "Cells")).Count;
    }

    static int[,] CloneCells(object board)
    {
        List<int[]> cells = (List<int[]>)Prop(board, "Cells");
        int rows = cells.Count;
        int cols = cells[0].Length;
        int[,] g = new int[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                g[r, c] = cells[r][c];
        return g;
    }

    // Lowest row at which the matrix still fits in the given column (-1 = never).
    static int LandingRow(object board, int[,] m, int col)
    {
        int last = -1;
        for (int row = 0; row <= Rows(board) - 5; row++)
        {
            bool ok = (bool)CallRet(board, "CanPlace", col, row, m);
            if (!ok) break;
            last = row;
        }
        return last;
    }

    // Dellacherie-style score of a hypothetical landing.
    // Playable columns are 2..cols-3; the two outermost ones are walls.
    static double Rate(int[,] g, int[,] m, int col, int row, out int completedLines)
    {
        int rows = g.GetLength(0);
        int cols = g.GetLength(1);

        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                if (m[i, j] != 0 && row + i < rows && col + j < cols)
                    g[row + i, col + j] = 1;

        completedLines = 0;
        for (int r = 0; r < rows; r++)
        {
            bool full = true;
            for (int c = 2; c < cols - 2; c++)
                if (g[r, c] == 0) { full = false; break; }
            if (full) completedLines++;
        }

        int aggregate = 0, holes = 0;
        int[] heights = new int[cols];
        for (int c = 2; c < cols - 2; c++)
        {
            int top = -1;
            for (int r = 0; r < rows; r++)
                if (g[r, c] != 0) { top = r; break; }
            if (top < 0) continue;
            heights[c] = rows - top;
            aggregate += heights[c];
            for (int r = top + 1; r < rows; r++)
                if (g[r, c] == 0) holes++;
        }

        int bumpiness = 0;
        for (int c = 2; c < cols - 3; c++)
            bumpiness += Math.Abs(heights[c] - heights[c + 1]);

        // Weights: clearing a line has to dominate, otherwise the evaluator
        // prefers a tidy low stack over an actual score.
        return -0.5 * aggregate + 8.0 * completedLines - 1.5 * holes - 0.3 * bumpiness;
    }

    // Choose and execute the best placement for the block that just appeared.
    static void PlaceNewBlock(object controller)
    {
        object board = Prop(controller, "Board");
        object block = Prop(board, "Current");

        int bestCol = 0, bestRot = 0;
        double bestScore = double.NegativeInfinity;

        object rotation = block;
        for (int rot = 0; rot < 4; rot++)
        {
            int[,] m = (int[,])Field(rotation, "Matrix");
            for (int col = 2; col <= 15; col++)
            {
                int row = LandingRow(board, m, col);
                if (row < 0) continue;
                int[,] g = CloneCells(board);
                int lines;
                double score = Rate(g, m, col, row, out lines);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCol = col;
                    bestRot = rot;
                }
            }
            rotation = CallRet(rotation, "Rotated");
        }

        for (int i = 0; i < bestRot; i++) Call(controller, "Rotate");

        // The rotation may have shifted the visible shape inside the matrix, so
        // re-read the column instead of assuming it stayed put.
        block = Prop(board, "Current");
        int cur = (int)Field(block, "Column");
        int guard = 0;
        while (cur < bestCol && guard++ < 20) { Call(controller, "Move", true); cur++; }
        while (cur > bestCol && guard++ < 20) { Call(controller, "Move", false); cur--; }

        if (verbose++ < 8)
            Console.WriteLine(string.Format(
                "    place: col={0} rot={1} score={2:F1} (landed col={3})",
                bestCol, bestRot, bestScore, cur));
    }

    static int verbose = 0;

    [STAThread]
    static int Main(string[] args)
    {
        string outDir = args.Length > 0 ? args[0] : "img";
        // The probe hosts the real assembly without running Program.Main,
        // so it has to resolve the language itself (env var / OS culture).
        owlgame.Strings.Configure(new string[0]);
        Directory.CreateDirectory(outDir);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        owlgame.MainForm form = new owlgame.MainForm();
        form.Show();
        Application.DoEvents();
        Thread.Sleep(200);
        Application.DoEvents();

        Save(form, Path.Combine(outDir, "welcome.png"));

        Button start = (Button)Field(form, "buttonstart");
        start.PerformClick();
        Application.DoEvents();

        bool midgameTaken = false;
        bool scoringTaken = false;
        int games = 0;
        object lastBlock = null;

        while (games < 150 && !(midgameTaken && scoringTaken))
        {
            games++;
            object controller = Field(form, "controller");
            if (controller == null) break;
            lastBlock = null;

            int ticks = 0;
            while (ticks++ < 6000)
            {
                object board = Prop(controller, "Board");
                object block = Prop(board, "Current");

                // A new block object means the previous one has been merged.
                if (!object.ReferenceEquals(block, lastBlock))
                {
                    lastBlock = block;
                    PlaceNewBlock(controller);
                }

                Tick(form);

                // Mid-game frame: enough blocks stacked that the picture shows
                // what the renderer actually produces, not an empty board.
                if (!midgameTaken && ticks >= 700)
                {
                    Save(form, Path.Combine(outDir, "playing.png"));
                    midgameTaken = true;
                }
                WaitForClear(controller);

                board = Prop(controller, "Board");
                int score = (int)Prop(board, "Score");
                int lines = (int)Prop(board, "Lines");

                if (!scoringTaken && lines >= 1 && score >= 100)
                {
                    Save(form, Path.Combine(outDir, "scoring.png"));
                    scoringTaken = true;
                }
                if ((bool)Call1(controller, "IsGameOver")) break;
            }

            if (midgameTaken && scoringTaken) break;
            Console.WriteLine(string.Format("  game {0}: ticks={1} score={2} lines={3}",
                games, ticks,
                (int)Prop(Prop(controller, "Board"), "Score"),
                (int)Prop(Prop(controller, "Board"), "Lines")));
            if ((bool)Call1(controller, "IsGameOver"))
            {
                start.PerformClick();
                Application.DoEvents();
            }
        }

        Console.WriteLine("games played: " + games
            + ", midgame=" + midgameTaken + ", scoring=" + scoringTaken);
        if (!midgameTaken && !scoringTaken)
        {
            Console.WriteLine("FAIL: never reached the target board state");
            return 1;
        }
        return 0;
    }
}
