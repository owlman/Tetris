using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

// Round-1 regression probe, rewritten after the round-3 refactor.
//
// The rules now live in GameBoard, which does not reference WinForms, so most
// checks can drive it directly instead of reaching into the controller with
// reflection. Only the two UI-side checks (link validation, button state) still
// need the form.
class ProbeRound1
{
    static owlgame.MainForm form;
    static System.Text.StringBuilder sb = new System.Text.StringBuilder();
    static int failures = 0;

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

    static string ShapeOf(owlgame.Block b)
    {
        string s = "";
        for (int i = 0; i < owlgame.Block.MatrixSize; i++)
        {
            for (int j = 0; j < owlgame.Block.MatrixSize; j++)
                s += (b.Matrix[i, j] == 1 ? "#" : ".");
            s += "|";
        }
        return s;
    }

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        form = new owlgame.MainForm();
        form.Show();
        Application.DoEvents();

        // ---- setup diagnostics must stay empty ----
        string diag = (string)typeof(owlgame.MainForm).GetField("diagnostics",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).GetValue(null);
        Check("no setup failure recorded", diag == null, "diagnostics=[" + (diag ?? "null") + "]");

        // ---- random shapes: two blocks created back to back must differ ----
        int samePairs = 0;
        for (int k = 0; k < 20; k++)
        {
            owlgame.Block a = new owlgame.Block(8, 0);
            owlgame.Block b = new owlgame.Block(8, 0);
            if (ShapeOf(a) == ShapeOf(b) && a.BlockColor == b.BlockColor) samePairs++;
        }
        // A few collisions are normal randomness (~2.9 of 20); before the fix
        // it was 20 / 20 every time.
        Check("back-to-back blocks differ", samePairs <= 8,
              "identical pairs = " + samePairs + " / 20 (random expectation ~2.9, before fix 20/20)");

        // ---- shape variety ----
        HashSet<string> shapes = new HashSet<string>();
        for (int k = 0; k < 60; k++) shapes.Add(ShapeOf(new owlgame.Block(8, 0)));
        Check("shape variety", shapes.Count >= 5, "distinct shapes = " + shapes.Count + " / 7");

        // ---- colour is derived from the shape ----
        Dictionary<string, Color> colourOf = new Dictionary<string, Color>();
        bool stable = true;
        string conflict = "";
        for (int k = 0; k < 200; k++)
        {
            owlgame.Block b = new owlgame.Block(8, 0);
            string sh = ShapeOf(b);
            if (colourOf.ContainsKey(sh))
            {
                if (colourOf[sh] != b.BlockColor) { stable = false; conflict = sh; break; }
            }
            else colourOf[sh] = b.BlockColor;
        }
        Check("shape -> colour stable", stable,
              "distinct shapes=" + colourOf.Count + (conflict == "" ? "" : " conflict=" + conflict));
        Check("seven colours", new HashSet<Color>(colourOf.Values).Count == 7,
              "distinct colours = " + new HashSet<Color>(colourOf.Values).Count);

        // ---- a fresh board must not show the same block twice ----
        bool anyDifferent = false;
        for (int k = 0; k < 10 && !anyDifferent; k++)
        {
            owlgame.GameBoard board = new owlgame.GameBoard(35);
            if (ShapeOf(board.Current) != ShapeOf(board.Next)
                || board.Current.BlockColor != board.Next.BlockColor)
                anyDifferent = true;
        }
        Check("board: current != next", anyDifferent, "checked 10 fresh boards");

        // ---- rules that are now testable without a message loop ----
        owlgame.GameBoard gb = new owlgame.GameBoard(35);
        int startRow = gb.Current.Row;
        gb.MoveDown(1);
        Check("MoveDown advances one line", gb.Current.Row == startRow + 1,
              "row " + startRow + " -> " + gb.Current.Row);

        // out-of-bounds requests must be refused, not crash
        bool refused;
        try { refused = !gb.CanPlace(-1, 0, gb.Current.Matrix); }
        catch (Exception ex) { refused = false; Log("       threw: " + ex.GetType().Name); }
        Check("CanPlace refuses column -1", refused, "no exception, returned false");

        bool refusedBelow;
        try { refusedBelow = !gb.CanPlace(8, 999, gb.Current.Matrix); }
        catch (Exception ex) { refusedBelow = false; Log("       threw: " + ex.GetType().Name); }
        Check("CanPlace refuses below the floor (used to throw)", refusedBelow,
              "no exception, returned false");

        // a full line must be cleared
        owlgame.GameBoard gb2 = new owlgame.GameBoard(35);
        for (int j = owlgame.GameBoard.WallThickness; j < owlgame.GameBoard.Columns - owlgame.GameBoard.WallThickness; j++)
            gb2.Cells[30][j] = owlgame.GameBoard.Settled;
        int cleared = gb2.ClearLines();
        Check("ClearLines removes a full line", cleared == 1, "cleared=" + cleared);

        // three lines in a row must all go
        owlgame.GameBoard gb3 = new owlgame.GameBoard(35);
        for (int line = 28; line <= 30; line++)
            for (int j = owlgame.GameBoard.WallThickness; j < owlgame.GameBoard.Columns - owlgame.GameBoard.WallThickness; j++)
                gb3.Cells[line][j] = owlgame.GameBoard.Settled;
        int cleared3 = gb3.ClearLines();
        Check("ClearLines removes three stacked lines", cleared3 == 3, "cleared=" + cleared3);

        // ---- fix 16: game over keeps the start button usable ----
        Button bs = (Button)Get(form, "buttonstart");
        bs.PerformClick();
        Application.DoEvents();
        object gc = Get(form, "controller");
        owlgame.GameBoard live = (owlgame.GameBoard)gc.GetType().GetProperty("Board").GetValue(gc, null);
        for (int j = owlgame.GameBoard.WallThickness + 1; j < owlgame.GameBoard.Columns - owlgame.GameBoard.WallThickness; j++)
            live.Cells[2][j] = owlgame.GameBoard.Settled;

        Timer t = (Timer)Get(form, "gametimer");
        t.Enabled = true;
        MethodInfo tick = typeof(owlgame.MainForm).GetMethod("gametimer_Tick",
            BindingFlags.Instance | BindingFlags.NonPublic);
        // push the block down until it lands, then run one tick
        while (live.MoveDown(1)) { }
        tick.Invoke(form, new object[] { null, EventArgs.Empty });
        Application.DoEvents();

        Check("start button enabled after game over", bs.Enabled,
              "Enabled=" + bs.Enabled + " text=[" + bs.Text + "]");
        Check("start caption restored", bs.Text == "开始(&E)", "text=[" + bs.Text + "]");

        // ---- fix 6: non-http(s) link must be refused ----
        System.Text.StringBuilder trace = new System.Text.StringBuilder();
        System.Diagnostics.Trace.Listeners.Add(new TextListener(trace));
        LinkLabel web = (LinkLabel)Get(form, "myweb");
        object oldTag = web.Tag;
        web.Tag = "ftp://example.com/payload";
        MethodInfo linkClick = typeof(owlgame.MainForm).GetMethod("myweb_LinkClicked",
            BindingFlags.Instance | BindingFlags.NonPublic);
        linkClick.Invoke(form, new object[] { web, null });
        web.Tag = oldTag;
        Check("non-http(s) link refused",
              trace.ToString().Contains("refused to open non-http(s) link"),
              "trace=[" + trace.ToString().Trim() + "]");

        form.Close();
        Log("");
        Log(failures == 0 ? "RESULT: ALL PASS" : "RESULT: " + failures + " FAILURE(S)");
        System.IO.File.WriteAllText("out_round1.txt", sb.ToString(),
            new System.Text.UTF8Encoding(false));
        return failures;
    }

    class TextListener : System.Diagnostics.TraceListener
    {
        private System.Text.StringBuilder _sb;
        public TextListener(System.Text.StringBuilder sb) { _sb = sb; }
        public override void Write(string message) { _sb.Append(message); }
        public override void WriteLine(string message) { _sb.AppendLine(message); }
    }
}
