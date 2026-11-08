using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

// Round-5 regression probe: scoring, level progression, drop speed and the
// clear-line flash.
//
// The score / level / speed rules live in GameBoard and are checked directly;
// the flash needs the form because it is driven by a timer.
class ProbeRound5
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

    // Fill one playable line completely.
    static void FillLine(owlgame.GameBoard b, int line)
    {
        for (int j = owlgame.GameBoard.WallThickness;
             j < owlgame.GameBoard.Columns - owlgame.GameBoard.WallThickness; j++)
            b.Cells[line][j] = owlgame.GameBoard.Settled;
    }

    static void FillLines(owlgame.GameBoard b, int first, int count)
    {
        for (int k = 0; k < count; k++) FillLine(b, first + k);
    }

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();

        // ---------- 1. scoring table ----------
        int[] expected = { 0, 100, 300, 500, 800 };
        for (int n = 1; n <= 4; n++)
        {
            owlgame.GameBoard b = new owlgame.GameBoard(35);
            // keep them away from the floor so the indices are valid
            FillLines(b, 20, n);
            int cleared = b.ClearLines();
            int score = b.Score;
            Check("score for " + n + " line(s)", cleared == n && score == expected[n],
                  "cleared=" + cleared + " score=" + score + " expected=" + expected[n]);
        }

        // ---------- 2. level progression ----------
        owlgame.GameBoard lb = new owlgame.GameBoard(35);
        Check("level starts at 1", lb.Level == 1, "level=" + lb.Level);
        // clear one line at a time, 9 times: still level 1
        for (int k = 0; k < 9; k++)
        {
            FillLine(lb, 20);
            lb.ClearLines();
        }
        Check("9 lines keeps level 1", lb.Level == 1,
              "lines=" + lb.Lines + " level=" + lb.Level);
        FillLine(lb, 20);
        lb.ClearLines();
        Check("10 lines reaches level 2", lb.Level == 2,
              "lines=" + lb.Lines + " level=" + lb.Level);

        // score is multiplied by the level at the time of the clear
        owlgame.GameBoard mb = new owlgame.GameBoard(35);
        for (int k = 0; k < 10; k++) { FillLine(mb, 20); mb.ClearLines(); }
        int before = mb.Score;
        FillLine(mb, 20);
        mb.ClearLines();
        Check("level 2 doubles the points", mb.Score - before == 200,
              "delta=" + (mb.Score - before) + " expected=200");

        // ---------- 3. drop speed follows the level ----------
        owlgame.GameBoard sb2 = new owlgame.GameBoard(35);
        int baseInterval = sb2.DropInterval;
        Check("level 1 drops at the base interval",
              baseInterval == owlgame.GameBoard.BaseDropInterval,
              "interval=" + baseInterval);
        for (int k = 0; k < 10; k++) { FillLine(sb2, 20); sb2.ClearLines(); }
        int lv2 = sb2.DropInterval;
        Check("level 2 drops faster", lv2 < baseInterval && lv2 > 0,
              "level=" + sb2.Level + " interval=" + lv2);

        // enough levels to hit the floor
        owlgame.GameBoard fast = new owlgame.GameBoard(35);
        for (int k = 0; k < 120; k++) { FillLine(fast, 20); fast.ClearLines(); }
        int clamped = fast.DropInterval;
        Check("drop interval is clamped at the minimum",
              clamped == owlgame.GameBoard.MinDropInterval,
              "level=" + fast.Level + " interval=" + clamped
              + " min=" + owlgame.GameBoard.MinDropInterval);

        // ---------- 4. clear-line flash drives a real timer ----------
        form = new owlgame.MainForm();
        form.Show();
        Application.DoEvents();

        Panel view = (Panel)Get(form, "gamewiew");
        Button bs = (Button)Get(form, "buttonstart");
        Label info = (Label)Get(form, "infoLabel");
        Timer clock = (Timer)Get(form, "gametimer");
        Timer flash = (Timer)Get(form, "flashTimer");
        MethodInfo tick = typeof(owlgame.MainForm).GetMethod("gametimer_Tick",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Check("info label exists", info != null, info == null ? "missing" : "ok");
        bs.PerformClick();
        Application.DoEvents();
        owlgame.GameController controller =
            (owlgame.GameController)Get(form, "controller");
        owlgame.GameBoard board = controller.Board;

        // a fresh game must show zeroes
        Log("info after start: " + info.Text.Replace(Environment.NewLine, "|"));
        Check("readout shows score 0", info.Text.Contains("0"), "text=[" + info.Text + "]");

        // Park the block on the floor, fill the line under it and let one tick
        // run: the turn must end in the "flashing" state, not in a clear.
        // Line 32 is the last clearable one: the floor starts below it.
        FillLine(board, 32);
        while (board.MoveDown(1)) { }
        clock.Enabled = false;
        tick.Invoke(form, new object[] { null, EventArgs.Empty });
        Application.DoEvents();

        Check("flash starts instead of clearing immediately",
              controller.IsHighlighting && flash.Enabled,
              "highlighting=" + controller.IsHighlighting + " flashTimer=" + flash.Enabled);

        // let the flash timer finish the job
        MethodInfo flashTick = typeof(owlgame.MainForm).GetMethod("flashTimer_Tick",
            BindingFlags.Instance | BindingFlags.NonPublic);
        flash.Enabled = false;
        flashTick.Invoke(form, new object[] { null, EventArgs.Empty });
        Application.DoEvents();

        Check("flash ends by clearing the line", !controller.IsHighlighting,
              "highlighting=" + controller.IsHighlighting);
        Check("line was scored", board.Score > 0, "score=" + board.Score);
        Log("info after clear: " + info.Text.Replace(Environment.NewLine, "|"));
        Check("readout shows the new score", info.Text.Contains(board.Score.ToString()),
              "score=" + board.Score + " text=[" + info.Text + "]");

        form.Close();
        Log("");
        Log(failures == 0 ? "RESULT: ALL PASS" : "RESULT: " + failures + " FAILURE(S)");
        System.IO.File.WriteAllText("out_round5.txt", sb.ToString(),
            new System.Text.UTF8Encoding(false));
        return failures;
    }
}
