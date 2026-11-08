using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

// Round-1 regression probe: verifies fixes 1 / 3 / 6 / 14 / 16.
class ProbeRound1
{
    static owlgame.mainForm form;
    static System.Text.StringBuilder sb = new System.Text.StringBuilder();
    static int failures = 0;

    static void Log(string s) { sb.AppendLine(s); }
    static void Check(string name, bool ok, string detail)
    {
        Log((ok ? "[PASS] " : "[FAIL] ") + name + "  " + detail);
        if (!ok) failures++;
    }

    static object Get(object target, string name, bool isStatic)
    {
        BindingFlags f = BindingFlags.Public | BindingFlags.NonPublic
                       | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
        FieldInfo fi = target == null ? typeof(owlgame.mainForm).GetField(name, f)
                                      : target.GetType().GetField(name, f);
        return fi == null ? null : fi.GetValue(target);
    }

    static string Shape(object blk)
    {
        int[,] m = (int[,])Get(blk, "matblock", false);
        string s = "";
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++) s += (m[i, j] == 1 ? "#" : ".");
            s += "|";
        }
        return s;
    }

    static Color ColorOf(object blk) { return (Color)Get(blk, "colorblock", false); }

    class TextListener : System.Diagnostics.TraceListener
    {
        private System.Text.StringBuilder _sb;
        public TextListener(System.Text.StringBuilder sb) { _sb = sb; }
        public override void Write(string message) { _sb.Append(message); }
        public override void WriteLine(string message) { _sb.AppendLine(message); }
    }

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        form = new owlgame.mainForm();
        form.Show();
        Application.DoEvents();

        // ---- fix 3: anti-flicker setup must not have failed silently ----
        string diag = (string)typeof(owlgame.mainForm).GetField("diagnostics",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).GetValue(null);
        Check("fix3 no setup failure recorded", diag == null, "diagnostics=[" + (diag ?? "null") + "]");

        Type blockType = typeof(owlgame.mainForm).Assembly.GetType("owlgame.block");
        Type ctrlType = typeof(owlgame.mainForm).Assembly.GetType("owlgame.gameController");

        // ---- fix 1: two blocks created back to back must differ ----
        int samePairs = 0;
        for (int k = 0; k < 20; k++)
        {
            object a = Activator.CreateInstance(blockType, new object[] { 8, 0 });
            object b = Activator.CreateInstance(blockType, new object[] { 8, 0 });
            if (Shape(a) == Shape(b) && ColorOf(a) == ColorOf(b)) samePairs++;
        }
        // 20 draws give a ~1/7 chance of two equal shapes, so a few collisions
        // are normal randomness. Before the fix it was 20 / 20 every time.
        Check("fix1 back-to-back blocks differ", samePairs <= 8,
              "identical pairs = " + samePairs + " / 20 (random expectation ~2.9, before fix 20/20)");

        // ---- fix 1b: rotation must not consume the random sequence ----
        // Shape distribution over 60 creations should cover more than 1 shape.
        System.Collections.Generic.HashSet<string> shapes =
            new System.Collections.Generic.HashSet<string>();
        for (int k = 0; k < 60; k++)
        {
            object b = Activator.CreateInstance(blockType, new object[] { 8, 0 });
            shapes.Add(Shape(b));
        }
        Check("fix1 shape variety", shapes.Count >= 5, "distinct shapes = " + shapes.Count + " / 7");

        // ---- fix 14: colour is derived from shape (same shape -> same colour) ----
        System.Collections.Generic.Dictionary<string, Color> map =
            new System.Collections.Generic.Dictionary<string, Color>();
        bool colourConsistent = true;
        string conflict = "";
        for (int k = 0; k < 200; k++)
        {
            object b = Activator.CreateInstance(blockType, new object[] { 8, 0 });
            string sh = Shape(b);
            Color c = ColorOf(b);
            if (map.ContainsKey(sh))
            {
                if (map[sh] != c) { colourConsistent = false; conflict = sh; break; }
            }
            else map[sh] = c;
        }
        Check("fix14 shape->colour stable", colourConsistent,
              "distinct shapes=" + map.Count + (conflict == "" ? "" : " conflict=" + conflict));

        System.Collections.Generic.HashSet<Color> colours =
            new System.Collections.Generic.HashSet<Color>(map.Values);
        Check("fix14 seven colours", colours.Count == 7, "distinct colours = " + colours.Count);

        // ---- fix 1 (game level): now != wait right after start ----
        Button bs = (Button)Get(form, "buttonstart", false);
        bs.PerformClick();
        Application.DoEvents();
        object gc = Get(form, "controller", false);
        object now = Get(gc, "now", false);
        object wait = Get(gc, "wait", false);
        Check("fix1 start: now != wait",
              Shape(now) != Shape(wait) || ColorOf(now) != ColorOf(wait),
              "now=" + Shape(now) + " wait=" + Shape(wait));

        // ---- fix 16: game over keeps start button usable ----
        // Force game over by filling the top row through the controller map.
        Timer t = (Timer)Get(form, "gametimer", false);
        System.Collections.IList board =
            (System.Collections.IList)Get(gc, "map", false);
        for (int j = 3; j < 18; j++) ((int[])board[2])[j] = 1;   // board[2] == row that triggers fall()
        t.Enabled = true;
        // Trigger one tick with a block that cannot move down.
        MethodInfo tick = typeof(owlgame.mainForm).GetMethod("gametimer_Tick",
            BindingFlags.Instance | BindingFlags.NonPublic);
        // Make the current block already resting on the pile: push it down until it stops.
        MethodInfo down = ctrlType.GetMethod("down");
        while ((bool)down.Invoke(gc, new object[] { 1 })) { }
        tick.Invoke(form, new object[] { null, EventArgs.Empty });
        Application.DoEvents();

        Button bstart = (Button)Get(form, "buttonstart", false);
        Label wl = (Label)Get(form, "welcomelabel", false);
        Check("fix16 start enabled after game over", bstart.Enabled,
              "Enabled=" + bstart.Enabled + " text=[" + bstart.Text + "]");
        Check("fix16 caption restored", bstart.Text == "开始(&E)", "text=[" + bstart.Text + "]");
        Log("       welcome label = [" + wl.Text + "]");
        Log("       timer enabled = " + t.Enabled);

        // ---- fix 6: non-http(s) Tag must be refused, not launched ----
        System.Text.StringBuilder traceLog = new System.Text.StringBuilder();
        System.Diagnostics.Trace.Listeners.Add(new TextListener(traceLog));
        LinkLabel web = (LinkLabel)Get(form, "myweb", false);
        object oldTag = web.Tag;
        web.Tag = "ftp://example.com/payload";
        MethodInfo linkClick = typeof(owlgame.mainForm).GetMethod("myweb_LinkClicked",
            BindingFlags.Instance | BindingFlags.NonPublic);
        linkClick.Invoke(form, new object[] { web, null });
        web.Tag = oldTag;
        Check("fix6 non-http(s) link refused",
              traceLog.ToString().Contains("refused to open non-http(s) link"),
              "trace=[" + traceLog.ToString().Trim() + "]");

        form.Close();

        Log("");
        Log(failures == 0 ? "RESULT: ALL PASS" : "RESULT: " + failures + " FAILURE(S)");
        System.IO.File.WriteAllText("out_round1.txt", sb.ToString(), new System.Text.UTF8Encoding(false));
        return failures;
    }
}
