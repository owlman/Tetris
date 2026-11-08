using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

// End-to-end smoke probe, adapted to the round-3 class split.
// Walls are still static dark grey and the board is still drawn from scratch on
// every repaint, so "number of grey pixels" stays a cheap picture-integrity
// signal: it drops the moment the board is erased mid-frame.
class ProbeSmoke
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

    struct Stats { public int gray, navy, other; }

    static Stats Sample(Control c)
    {
        Stats s = new Stats();
        using (Bitmap bmp = new Bitmap(c.Width, c.Height))
        {
            c.DrawToBitmap(bmp, new Rectangle(0, 0, c.Width, c.Height));
            for (int y = 0; y < c.Height; y++)
                for (int x = 0; x < c.Width; x++)
                {
                    Color p = bmp.GetPixel(x, y);
                    if (p.R == 128 && p.G == 128 && p.B == 128) s.gray++;
                    else if (p.R == 0 && p.G == 0 && p.B == 64) s.navy++;
                    else s.other++;
                }
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

        Panel view = (Panel)Get(form, "gamewiew");
        Button bs = (Button)Get(form, "buttonstart");
        Button bb = (Button)Get(form, "buttonback");

        bs.PerformClick();
        Application.DoEvents();
        view.Invalidate();
        Application.DoEvents();

        Stats start = Sample(view);
        int startColored = start.other;
        Log("after start  : gray=" + start.gray + " colored=" + startColored
            + " navy=" + start.navy);
        Check("walls painted", start.gray > 10000, "gray=" + start.gray);
        Check("current block painted", startColored > 0, "colored=" + startColored);

        owlgame.GameController controller =
            (owlgame.GameController)Get(form, "controller");
        Random rnd = new Random(12345);

        int minGray = int.MaxValue, maxGray = 0;
        int minColored = int.MaxValue, maxColored = 0;
        int frames = 0;
        for (int k = 0; k < 120; k++)
        {
            int op = rnd.Next(3);
            if (op == 0) controller.Move(rnd.Next(2) == 0);
            else if (op == 1) controller.Rotate();
            else controller.Down(1);
            view.Invalidate();
            Application.DoEvents();

            Stats s = Sample(view);
            frames++;
            if (s.gray < minGray) minGray = s.gray;
            if (s.gray > maxGray) maxGray = s.gray;
            int colored = s.other;
            if (colored < minColored) minColored = colored;
            if (colored > maxColored) maxColored = colored;
        }
        Log("120 key ops  : gray min=" + minGray + " max=" + maxGray
            + " colored min=" + minColored + " max=" + maxColored
            + " (frames sampled=" + frames + ")");
        Check("walls never erased (no flicker)", minGray > 10000, "min gray=" + minGray);
        Check("walls pixel drop count stable",
              minGray == maxGray || (maxGray - minGray) < 200,
              "min=" + minGray + " max=" + maxGray);

        // Restart path must still work after the game-over change.
        bb.PerformClick();
        Application.DoEvents();
        view.Invalidate();
        Application.DoEvents();
        Stats restart = Sample(view);
        int restartColored = restart.other;
        Log("after restart: gray=" + restart.gray + " colored=" + restartColored
            + " navy=" + restart.navy);
        Check("restart keeps walls", restart.gray > 10000, "gray=" + restart.gray);
        Check("restart repaints block", restartColored > 0, "colored=" + restartColored);

        // Preview pane must still be painted (the Opaque contract lesson).
        PictureBox pro = (PictureBox)Get(form, "proimg");
        using (Bitmap bmp = new Bitmap(pro.Width, pro.Height))
        {
            pro.DrawToBitmap(bmp, new Rectangle(0, 0, pro.Width, pro.Height));
            Color corner = bmp.GetPixel(0, 0);
            Color inside = bmp.GetPixel(5, 12);
            Log("preview      : corner=" + corner + " inside=" + inside);
            Check("preview frame", corner.R == 128 && corner.G == 128, "corner=" + corner);
            Check("preview pane navy", inside.B == 64 && inside.R == 0, "inside=" + inside);
        }

        form.Close();
        Log("");
        Log(failures == 0 ? "RESULT: ALL PASS" : "RESULT: " + failures + " FAILURE(S)");
        System.IO.File.WriteAllText("out_smoke.txt", sb.ToString(),
            new System.Text.UTF8Encoding(false));
        return failures;
    }
}
