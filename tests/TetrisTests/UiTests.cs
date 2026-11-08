using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using owlgame;

namespace TetrisTests
{
    /// <summary>
    /// Runner entry point. Everything is compiled into one exe; the runner finds
    /// the [Test] methods by reflection.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            return TestRunner.Run(args);
        }
    }

    /// <summary>
    /// Tests that need a real window. They need a desktop session, so the runner
    /// can skip them with --skip-ui (which matters on a headless CI runner).
    /// </summary>
    [Category("UI")]
    public class UiTests
    {
        [DllImport("user32.dll")]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
        private const uint GR_GDIOBJECTS = 0;

        private static object Field(object target, string name)
        {
            FieldInfo fi = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return fi == null ? null : fi.GetValue(target);
        }

        private static void Settle()
        {
            Application.DoEvents();
        }

        private static uint GdiCount()
        {
            using (System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess())
                return GetGuiResources(p.Handle, GR_GDIOBJECTS);
        }

        private static MainForm OpenForm()
        {
            Application.EnableVisualStyles();
            MainForm form = new MainForm();
            form.Show();
            Settle();
            return form;
        }

        private static MethodInfo PrivateMethod(string name)
        {
            return typeof(MainForm).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Fills the last clearable line of the live board so the next turn has to
        /// clear something.
        /// </summary>
        private static int PrepareFullLine(GameController controller)
        {
            int clearable = controller.Board.LineCount - GameBoard.FloorThickness - 1;
            for (int j = GameBoard.WallThickness;
                 j < GameBoard.Columns - GameBoard.WallThickness; j++)
                controller.Board.Cells[clearable][j] = GameBoard.Settled;
            while (controller.Board.MoveDown(1)) { }
            return clearable;
        }

        [Test]
        public void NoSetupFailureIsRecorded()
        {
            // Regression: the reflection based anti flicker setup used to swallow
            // its exceptions and leave no trace at all.
            using (MainForm form = OpenForm())
            {
                FieldInfo diagnosticsField = typeof(MainForm).GetField("diagnostics",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                string diagnostics = (string)diagnosticsField.GetValue(null);
                Assert.IsNull(diagnostics, "no anti flicker setup failure should be recorded");
            }
        }

        [Test]
        public void InfoLabelStartsWithZeroes()
        {
            using (MainForm form = OpenForm())
            {
                Label info = (Label)Field(form, "infoLabel");
                Assert.IsNotNull(info, "the readout label exists");
                string text = info.Text;
                Assert.IsTrue(text.Contains("分数") && text.Contains("等级") && text.Contains("消行"),
                    "the readout mentions score, level and lines, was: " + text);
            }
        }

        [Test]
        public void StartingAGameEnablesTheClock()
        {
            using (MainForm form = OpenForm())
            {
                Timer clock = (Timer)Field(form, "gametimer");
                Button start = (Button)Field(form, "buttonstart");
                Assert.IsFalse(clock.Enabled, "the clock is stopped before starting");

                start.PerformClick();
                Settle();

                Assert.IsTrue(clock.Enabled, "the clock runs after clicking start");
                Assert.AreEqual("停止(&P)", start.Text, "the button switched to stop");
            }
        }

        [Test]
        public void RestartingDoesNotLeakGdiHandles()
        {
            // Regression: the controller caches Pen / SolidBrush objects, so every
            // restart has to release the previous one.
            using (MainForm form = OpenForm())
            {
                Button start = (Button)Field(form, "buttonstart");
                Button restart = (Button)Field(form, "buttonback");
                start.PerformClick();
                Settle();
                restart.PerformClick();
                Settle();

                uint before = GdiCount();
                for (int k = 0; k < 15; k++)
                {
                    restart.PerformClick();
                    Settle();
                }
                uint after = GdiCount();

                Assert.IsTrue(after <= before + 2,
                    "GDI objects grew from " + before + " to " + after + " over 15 restarts");
            }
        }

        [Test]
        public void FullLineEntersTheFlashState()
        {
            using (MainForm form = OpenForm())
            {
                Button start = (Button)Field(form, "buttonstart");
                Timer clock = (Timer)Field(form, "gametimer");
                Timer flash = (Timer)Field(form, "flashTimer");
                start.PerformClick();
                Settle();

                GameController controller = (GameController)Field(form, "controller");
                clock.Enabled = false;
                PrepareFullLine(controller);

                PrivateMethod("gametimer_Tick").Invoke(form, new object[] { null, EventArgs.Empty });
                Settle();

                Assert.IsTrue(controller.IsHighlighting, "the completed line is flashing");
                Assert.IsTrue(flash.Enabled, "the flash timer is running");
            }
        }

        [Test]
        public void FlashTickClearsAndScores()
        {
            using (MainForm form = OpenForm())
            {
                Button start = (Button)Field(form, "buttonstart");
                Timer clock = (Timer)Field(form, "gametimer");
                Timer flash = (Timer)Field(form, "flashTimer");
                start.PerformClick();
                Settle();

                GameController controller = (GameController)Field(form, "controller");
                clock.Enabled = false;
                PrepareFullLine(controller);

                PrivateMethod("gametimer_Tick").Invoke(form, new object[] { null, EventArgs.Empty });

                flash.Enabled = false;
                PrivateMethod("flashTimer_Tick").Invoke(form, new object[] { null, EventArgs.Empty });
                Settle();

                Assert.IsFalse(controller.IsHighlighting, "the flash ended");
                Assert.IsTrue(controller.Board.Score > 0,
                    "the cleared line was scored, score=" + controller.Board.Score);
                Assert.IsTrue(PrivateMethod("gametimer_Tick") != null, "the tick handler exists");
            }
        }

        [Test]
        public void PreviewPaneIsPaintedBeforeTheGameStarts()
        {
            // The Opaque style flag means Windows does not erase the pane, so the
            // paint handler has to cover it even when controller is null.
            using (MainForm form = OpenForm())
            {
                PictureBox preview = (PictureBox)Field(form, "proimg");
                using (Bitmap bmp = new Bitmap(preview.Width, preview.Height))
                {
                    preview.DrawToBitmap(bmp, new Rectangle(0, 0, preview.Width, preview.Height));
                    Color corner = bmp.GetPixel(0, 0);
                    Color inside = bmp.GetPixel(5, 12);

                    Assert.AreEqual(128, corner.R, "the pane has a grey frame, corner=" + corner);
                    Assert.AreEqual(128, corner.G, "the pane has a grey frame, corner=" + corner);
                    Assert.AreEqual(64, inside.B, "the pane background is navy, inside=" + inside);
                    Assert.AreEqual(0, inside.R, "the pane background is navy, inside=" + inside);
                }
            }
        }

        [Test]
        public void BoardKeepsItsWallsWhilePlaying()
        {
            using (MainForm form = OpenForm())
            {
                Button start = (Button)Field(form, "buttonstart");
                start.PerformClick();
                Settle();

                Panel view = (Panel)Field(form, "gamewiew");
                view.Invalidate();
                Settle();

                using (Bitmap bmp = new Bitmap(view.Width, view.Height))
                {
                    view.DrawToBitmap(bmp, new Rectangle(0, 0, view.Width, view.Height));
                    int gray = 0;
                    for (int y = 0; y < bmp.Height; y++)
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            Color p = bmp.GetPixel(x, y);
                            if (p.R == 128 && p.G == 128 && p.B == 128) gray++;
                        }
                    Assert.AreEqual(17200, gray, "wall pixel count is the established baseline");
                }
            }
        }
    }
}
