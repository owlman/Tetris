using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace owlgame
{
    public partial class MainForm : Form
    {
        private GameController controller;

        /// <summary>How many fade steps the clear animation runs for.</summary>
        private const int FlashSteps = 3;

        private int flashStep;

        /// <summary>
        /// Lines waiting for the clear animation, or null. Kept in a field rather
        /// than in Timer.Tag so that CancelPendingClear() can reliably drop it:
        /// the timer used to be stoppable only from its own Tick handler, which
        /// meant a stop / restart / game-over during the animation left it
        /// running against a board that no longer existed.
        /// </summary>
        private System.Collections.Generic.List<int> pendingClear;

        // Diagnostic channel. Reflection based styling and link handling are
        // best effort, but a silent failure would hide the flicker quietly
        // coming back. Trace.WriteLine alone is not enough: it carries
        // [Conditional("TRACE")], so the call is removed entirely unless TRACE
        // is defined. This field is observable in every build.
        internal static string diagnostics = null;
        public MainForm()
        {            
            InitializeComponent();
            buttonback.Enabled = false;

            // Anti flicker setup. See ApplyFlickerFix() below.
            EnableDoubleBuffer(this);
            ApplyFlickerFix(gamewiew);
            ApplyFlickerFix(proimg);

            // The controller caches GDI objects now, so it has to be disposed.
            this.FormClosing += delegate
            {
                CancelPendingClear();
                if (controller != null)
                {
                    controller.Dispose();
                    controller = null;
                }
            };
        }

        /// <summary>
        /// Replace the controller with a fresh one, releasing the cached GDI
        /// objects of the previous game (they used to leak on every restart).
        /// </summary>
        private void ResetController()
        {
            // A restart must not inherit a half finished clear animation from the
            // previous game.
            CancelPendingClear();

            if (controller != null)
            {
                controller.Dispose();
                controller = null;
            }
            controller = new GameController(gamewiew);
        }

        /// <summary>
        /// Drop a running clear animation without clearing anything. The game is
        /// either being reset or stopped, so the pending lines are irrelevant.
        /// </summary>
        private void CancelPendingClear()
        {
            pendingClear = null;
            flashStep = 0;
            flashTimer.Enabled = false;
            flashTimer.Tag = null;
            if (controller != null)
                controller.HighlightLines(null);
        }

        /// <summary>
        /// Finish a running clear animation immediately: the pending lines are
        /// removed and the turn is completed. Used when something has to pause the
        /// game (opening the help dialog) - cancelling instead would leave a full
        /// line on the board with the clock stopped, i.e. a stuck game.
        /// </summary>
        private void CompletePendingClear()
        {
            if (pendingClear == null)
                return;

            System.Collections.Generic.List<int> lines = pendingClear;
            CancelPendingClear();

            if (controller == null)
                return;

            controller.ClearLines(lines);
            gamewiew.Invalidate();
            UpdateStatus();
        }

        /// <summary>
        /// Enable double buffering on a control and on all of its children.
        /// Control.DoubleBuffered is protected, so it can not be set from the
        /// designer; reflection is used instead.
        /// </summary>
        private static void EnableDoubleBuffer(Control control)
        {
            if (control == null) return;
            try
            {
                System.Reflection.PropertyInfo prop = typeof(Control).GetProperty(
                    "DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (prop != null)
                    prop.SetValue(control, true, null);
            }
            catch (Exception ex)
            {
                ReportSetupFailure("DoubleBuffered", ex);
            }

            foreach (Control child in control.Controls)
                EnableDoubleBuffer(child);
        }

        /// <summary>
        /// Remove the flicker of a self painted control.
        ///
        /// A normal control is repainted in two steps: Windows first sends
        /// WM_ERASEBKGND (background wipe) and only afterwards WM_PAINT (the
        /// real drawing). While playing the falling block moves on every key
        /// press, so that two step repaint happens dozens of times per second
        /// and the wipe becomes visible - that is the flicker.
        ///
        /// Two style flags remove it:
        ///   AllPaintingInWmPaint - do not treat WM_ERASEBKGND as an erase
        ///                          request, paint everything in WM_PAINT.
        ///   Opaque               - the control paints every pixel itself, so
        ///                          Windows must not erase it at all.
        /// Together with DoubleBuffered the frame is composed off screen and
        /// shown in one go, so no intermediate state is ever visible.
        ///
        /// Safe here because gameController.draw() / drawwait() start by
        /// filling the whole client area with the background colour.
        /// </summary>
        private static void ApplyFlickerFix(Control control)
        {
            if (control == null) return;
            EnableDoubleBuffer(control);
            try
            {
                System.Reflection.MethodInfo setStyle = typeof(Control).GetMethod(
                    "SetStyle",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (setStyle != null)
                {
                    ControlStyles styles = ControlStyles.OptimizedDoubleBuffer
                                         | ControlStyles.AllPaintingInWmPaint
                                         | ControlStyles.Opaque;
                    setStyle.Invoke(control, new object[] { styles, true });
                }
            }
            catch (Exception ex)
            {
                ReportSetupFailure("SetStyle", ex);
            }
        }

        private static void ReportSetupFailure(string member, Exception ex)
        {
            ReportProblem(member + ": " + ex.GetType().Name + ": " + ex.Message, true);
        }

        /// <summary>
        /// Append a problem to the diagnostic channel (always observable), echo
        /// it to Trace when the build defines TRACE, and optionally alert the
        /// user. Set OWLGAME_DIAG=1 in the environment to enable the alert.
        /// </summary>
        private static void ReportProblem(string message, bool alert)
        {
            if (diagnostics == null)
                diagnostics = message;
            else
                diagnostics += " | " + message;

            System.Diagnostics.Trace.WriteLine("[owlgame] " + message);

            if (alert && Environment.GetEnvironmentVariable("OWLGAME_DIAG") == "1")
                MessageBox.Show(message, "owlgame diagnostics",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void myweb_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (this.myweb.Tag == null)
                return;

            string url = this.myweb.Tag.ToString();
            Uri uri;
            // Only hand http / https to the shell. A Tag holding a local path
            // or a custom scheme would otherwise launch arbitrary programs.
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                ReportProblem("refused to open non-http(s) link: " + url, false);
                return;
            }

            System.Diagnostics.Process.Start(uri.AbsoluteUri);
        }

        private void buttonstart_Click(object sender, EventArgs e)
        {
            if (buttonstart.Text == "开始(&E)")
            {
                gaizi.Hide();
                ResetController();
                buttonback.Enabled = true;
                ApplyDropSpeed();
                gametimer.Enabled = true;
                UpdateStatus();
                gamewiew.Invalidate();
                proimg.Invalidate();
                buttonstart.Text = "停止(&P)";
                gamewiew.Focus();
            }
            else
            {
                buttonback.Enabled = false;
                gametimer.Enabled = false;
                // Without this the animation would finish and EndTurn would turn
                // the clock back on, restarting a game the user just stopped.
                CancelPendingClear();
                gaizi.Show();
                buttonstart.Text = "开始(&E)";
                welcomelabel.Text = "欢迎尝试游戏";
                proimg.Invalidate();
                gaizi.Focus();

            }
            
        }

        private void gamewiew_Paint(object sender, PaintEventArgs e)
        {
            // Always repaint, so the back buffer always matches what is shown
            // on screen (no ghosting / flickering when the timer is stopped).
            if (controller != null)
                controller.Draw(e);
        }

        private void gametimer_Tick(object sender, EventArgs e)
        {
            // The block is still falling: nothing else to do this tick.
            if (controller.Down(1))
                return;

            controller.Merge();

            System.Collections.Generic.List<int> full = controller.FindFullLines();
            if (full.Count > 0)
            {
                // Show the completed lines for a moment, then clear them in
                // flashTimer_Tick. The clock is stopped meanwhile so the next
                // block cannot start falling over the animation.
                gametimer.Enabled = false;
                controller.HighlightLines(full);
                gamewiew.Invalidate();
                pendingClear = full;
                flashStep = 0;
                flashTimer.Enabled = true;
                return;
            }

            EndTurn();
        }

        /// <summary>
        /// Completes the turn once no line is waiting to be cleared: promote the
        /// preview block, refresh the readout and look for the game over state.
        /// </summary>
        private void EndTurn()
        {
            controller.Advance();
            UpdateStatus();

            if (controller.IsGameOver())
            {
                gametimer.Enabled = false;
                CancelPendingClear();
                gaizi.Show();
                welcomelabel.Text = "你失败了… 分数 " + controller.Board.Score;
                // Keep the start button usable and restore its caption so a
                // single click starts the next round; it used to be disabled,
                // leaving "重玩" as the only way out.
                buttonstart.Enabled = true;
                buttonstart.Text = "开始(&E)";
                gaizi.Focus();
                gamewiew.Invalidate();
                return;
            }

            ApplyDropSpeed();
            gametimer.Enabled = true;
            proimg.Invalidate();
            gamewiew.Invalidate();
        }

        private void flashTimer_Tick(object sender, EventArgs e)
        {
            // Fade the highlighted lines for a few steps, then remove them.
            flashStep++;
            if (flashStep < FlashSteps)
            {
                controller.AdvanceFlash();
                gamewiew.Invalidate();
                return;
            }

            flashTimer.Enabled = false;
            System.Collections.Generic.List<int> full = pendingClear;
            pendingClear = null;

            if (full != null && controller != null)
                controller.ClearLines(full);

            gamewiew.Invalidate();
            EndTurn();
        }

        /// <summary>
        /// Keep the drop interval in step with the level: level 1 drops once a
        /// second, every level is a little faster.
        /// </summary>
        private void ApplyDropSpeed()
        {
            int interval = controller.Board.DropInterval;
            if (gametimer.Interval != interval)
                gametimer.Interval = interval;
        }

        /// <summary>Refresh the score / level / lines readout.</summary>
        private void UpdateStatus()
        {
            if (controller == null)
            {
                scoreValue.Text = "0";
                levelValue.Text = "1";
                linesValue.Text = "0";
                return;
            }

            GameBoard board = controller.Board;
            scoreValue.Text = board.Score.ToString();
            levelValue.Text = board.Level.ToString();
            linesValue.Text = board.Lines.ToString();
        }

        private void buttonback_Click(object sender, EventArgs e)
        {
            gaizi.Hide();
            ResetController();
            buttonstart.Enabled = true;
            ApplyDropSpeed();
            gametimer.Enabled = true;
            UpdateStatus();
            gamewiew.Invalidate();
            proimg.Invalidate();
            buttonstart.Text = "停止(&P)";
            gamewiew.Focus();
        }

        private void gamewiew_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {           
            switch (e.KeyCode)
            {
                case Keys.Up:
                    controller.Rotate();
                    break;
                case Keys.Left:
                    controller.Move(false);
                    break;
                case Keys.Right:
                    controller.Move(true);
                    break;
                case Keys.Down:
                    controller.Down(2);
                    break;
            }           
        }

        private void proimg_Paint(object sender, PaintEventArgs e)
        {
            // The preview pane must be painted even before the game starts.
            // proimg carries the Opaque style flag (see ApplyFlickerFix), so
            // Windows does not erase its background for us: if we paint
            // nothing here the whole area keeps whatever was underneath and
            // the "next" box shows no pane at all on the initial screen.
            using (SolidBrush bgBrush = new SolidBrush(proimg.BackColor))
                e.Graphics.FillRectangle(bgBrush, proimg.ClientRectangle);
            if (controller != null)
                controller.DrawPreview(proimg, e);
            // No outline any more: the preview sits inside a white card, and a
            // second grey frame around it just added noise.
        }

        private void buttonset_Click(object sender, EventArgs e)
        {
            if (gametimer.Enabled)
            {
                // Settle a running clear animation first: leaving it pending while
                // the dialog runs would remove the lines in the background and
                // re-enable the clock behind a modal window.
                CompletePendingClear();
                gametimer.Enabled = false;
                if ((new gamehelp()).ShowDialog() == DialogResult.OK)
                    gametimer.Enabled = true;
                gamewiew.Focus();
            }
            else
            {
                (new gamehelp()).ShowDialog();
                gaizi.Focus();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            gaizi.Focus();
        }

        private void buttonexit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}