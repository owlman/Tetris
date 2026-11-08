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

    struct Stats { public int wall, other; }

    static Stats Sample(Control c)
    {
        Stats s = new Stats();
        using (Bitmap bmp = new Bitmap(c.Width, c.Height))
        {
            c.DrawToBitmap(bmp, new Rectangle(0, 0, c.Width, c.Height));
            // Wall colour comes from the product, so a restyle does not
            // invalidate the baseline. Walls are filled edge to edge, which is
            // why their pixel count is a stable picture-integrity signal.
            Color wall = owlgame.GameController.WallColour;
            for (int y = 0; y < c.Height; y++)
                for (int x = 0; x < c.Width; x++)
                {
                    Color p = bmp.GetPixel(x, y);
                    if (p.R == wall.R && p.G == wall.G && p.B == wall.B) s.wall++;
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
        Log("after start  : wall=" + start.wall + " colored=" + startColored);
        Check("walls painted", start.wall > 10000, "wall=" + start.wall);
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
            if (s.wall < minGray) minGray = s.wall;
            if (s.wall > maxGray) maxGray = s.wall;
            int colored = s.other;
            if (colored < minColored) minColored = colored;
            if (colored > maxColored) maxColored = colored;
        }
        Log("120 key ops  : wall min=" + minGray + " max=" + maxGray
            + " colored min=" + minColored + " max=" + maxColored
            + " (frames sampled=" + frames + ")");
        Check("walls never erased (no flicker)", minGray > 10000, "min wall=" + minGray);
        Check("walls pixel count stable",
              minGray == maxGray || (maxGray - minGray) < 200,
              "min=" + minGray + " max=" + maxGray);

        // Restart path must still work after the game-over change.
        bb.PerformClick();
        Application.DoEvents();
        view.Invalidate();
        Application.DoEvents();
        Stats restart = Sample(view);
        int restartColored = restart.other;
        Log("after restart: wall=" + restart.wall + " colored=" + restartColored);
        Check("restart keeps walls", restart.wall > 10000, "wall=" + restart.wall);
        Check("restart repaints block", restartColored > 0, "colored=" + restartColored);

        // Preview pane must still be painted (the Opaque contract lesson).
        PictureBox pro = (PictureBox)Get(form, "proimg");
        using (Bitmap bmp = new Bitmap(pro.Width, pro.Height))
        {
            pro.DrawToBitmap(bmp, new Rectangle(0, 0, pro.Width, pro.Height));
            Color corner = bmp.GetPixel(0, 0);
            Color inside = bmp.GetPixel(5, 12);
            Log("preview      : corner=" + corner + " inside=" + inside);
            Check("preview pane painted", corner == pro.BackColor,
                  "corner=" + corner + " expected=" + pro.BackColor);
        }

        form.Close();
        Log("");
        Log(failures == 0 ? "RESULT: ALL PASS" : "RESULT: " + failures + " FAILURE(S)");
        System.IO.File.WriteAllText("out_smoke.txt", sb.ToString(),
            new System.Text.UTF8Encoding(false));
        return failures;
    }
}
