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
        public void ReadoutStartsWithZeroes()
        {
            // The single multi-line infoLabel was replaced by three value labels
            // plus three captions when the side panel was modernised.
            using (MainForm form = OpenForm())
            {
                Label score = (Label)Field(form, "scoreValue");
                Label level = (Label)Field(form, "levelValue");
                Label lines = (Label)Field(form, "linesValue");

                Assert.IsNotNull(score, "score value label exists");
                Assert.IsNotNull(level, "level value label exists");
                Assert.IsNotNull(lines, "lines value label exists");

                Assert.AreEqual("0", score.Text, "score starts at zero");
                Assert.AreEqual("1", level.Text, "level starts at one");
                Assert.AreEqual("0", lines.Text, "no lines cleared yet");
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

                // The clear animation fades over several ticks before it removes
                // the line, so drive it until the highlight is gone.
                int guard = 0;
                while (controller.IsHighlighting)
                {
                    if (++guard > 10)
                        Assert.Fail("the clear animation never finished");
                    flash.Enabled = false;
                    PrivateMethod("flashTimer_Tick").Invoke(form, new object[] { null, EventArgs.Empty });
                    Settle();
                }

                Assert.IsFalse(controller.IsHighlighting, "the flash ended");
                Assert.IsTrue(controller.Board.Score > 0,
                    "the cleared line was scored, score=" + controller.Board.Score);
                Assert.IsTrue(PrivateMethod("gametimer_Tick") != null, "the tick handler exists");
            }
        }

        [Test]
        public void ControllerExposesTheCellGeometry()
        {
            using (MainForm form = OpenForm())
            {
                ((Button)Field(form, "buttonstart")).PerformClick();
                Settle();

                GameController controller = (GameController)Field(form, "controller");
                Panel view = (Panel)Field(form, "gamewiew");

                Assert.AreEqual(view.Width / GameBoard.Columns, controller.CellWidth,
                    "cell width is the view width divided by the column count");

                int expectedLines = GameBoard.Columns * view.Height / view.Width;
                Assert.AreEqual(view.Height / expectedLines, controller.CellHeight,
                    "cell height is the view height divided by the line count");
            }
        }

        [Test]
        public void ControllerDelegatesTheBoardRules()
        {
            using (MainForm form = OpenForm())
            {
                Button start = (Button)Field(form, "buttonstart");
                Timer clock = (Timer)Field(form, "gametimer");
                start.PerformClick();
                Settle();
                clock.Enabled = false;

                GameController controller = (GameController)Field(form, "controller");
                GameBoard board = controller.Board;

                // Down / Move / Rotate
                int row = board.Current.Row;
                Assert.IsTrue(controller.Down(1), "Down delegates to MoveDown");
                Assert.AreEqual(row + 1, board.Current.Row, "row advanced by Down");

                int column = board.Current.Column;
                controller.Move(true);
                Assert.AreEqual(column + 1, board.Current.Column, "column advanced by Move");

                Assert.IsFalse(controller.IsGameOver(), "a fresh board is playable");

                // FindFullLines
                Assert.AreEqual(0, controller.FindFullLines().Count,
                    "FindFullLines reports nothing on an empty board");

                // HighlightLines / IsHighlighting / ClearLines(IList)
                System.Collections.Generic.List<int> lines =
                    new System.Collections.Generic.List<int>();
                lines.Add(20);
                Assert.IsFalse(controller.IsHighlighting, "no highlight before asking");
                controller.HighlightLines(lines);
                Assert.IsTrue(controller.IsHighlighting, "highlighting after HighlightLines");
                Assert.AreEqual(1, controller.ClearLines(lines), "the listed line was removed");
                Assert.IsFalse(controller.IsHighlighting, "ClearLines drops the highlight");

                // Merge
                while (board.MoveDown(1)) { }
                int settledBefore = CountSettled(board);
                controller.Merge();
                Assert.AreEqual(settledBefore + 4, CountSettled(board),
                    "Merge writes the block into the board");

                // Advance
                Block previousNext = board.Next;
                controller.Advance();
                Assert.IsSame(previousNext, board.Current, "Advance promotes the preview block");
            }
        }

        private static int CountSettled(GameBoard board)
        {
            int n = 0;
            for (int i = 0; i < board.LineCount; i++)
                for (int j = 0; j < GameBoard.Columns; j++)
                    if (board.CellAt(i, j) == GameBoard.Settled)
                        n++;
            return n;
        }

        [Test]
        public void PreviewPaneIsPaintedBeforeTheGameStarts()
        {
            // The Opaque style flag means Windows does not erase the pane, so the
            // paint handler has to cover it even when controller is null.
            using (MainForm form = OpenForm())
            {
                // Expectations come from the product, not from colour literals.
                PictureBox preview = (PictureBox)Field(form, "proimg");
                using (Bitmap bmp = new Bitmap(preview.Width, preview.Height))
                {
                    preview.DrawToBitmap(bmp, new Rectangle(0, 0, preview.Width, preview.Height));
                    Color corner = bmp.GetPixel(0, 0);

                    Assert.AreEqual(preview.BackColor, corner,
                        "the pane is painted with its own background colour, corner=" + corner);
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
                GameController controller = (GameController)Field(form, "controller");
                view.Invalidate();
                Settle();

                // Walls are filled edge to edge (no gap), so the expected count is
                // "wall cells x cell area". Deriving it means a restyle only has to
                // update the palette, not this test.
                int wallCells = 0;
                for (int i = 0; i < controller.Board.LineCount; i++)
                    for (int j = 0; j < GameBoard.Columns; j++)
                        if (controller.Board.CellAt(i, j) == GameBoard.Wall)
                            wallCells++;

                Assert.IsTrue(wallCells > 100, "the board should have plenty of wall cells");
                int expected = wallCells * controller.CellWidth * controller.CellHeight;

                using (Bitmap bmp = new Bitmap(view.Width, view.Height))
                {
                    view.DrawToBitmap(bmp, new Rectangle(0, 0, view.Width, view.Height));
                    int wallPixels = 0;
                    Color wallColour = GameController.WallColour;
                    for (int y = 0; y < bmp.Height; y++)
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            Color p = bmp.GetPixel(x, y);
                            if (p.R == wallColour.R && p.G == wallColour.G && p.B == wallColour.B)
                                wallPixels++;
                        }
                    Assert.AreEqual(expected, wallPixels,
                        "every wall cell should be filled with the wall colour");
                }
            }
        }
    }
}
