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
    public partial class mainForm : Form
    {
        private gameController controller;
        public mainForm()
        {            
            InitializeComponent();
            buttonback.Enabled = false;

            // Anti flicker setup. See ApplyFlickerFix() below.
            EnableDoubleBuffer(this);
            ApplyFlickerFix(gamewiew);
            ApplyFlickerFix(proimg);
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
            catch (Exception) { }

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
            catch (Exception) { }
        }

        private void myweb_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (this.myweb.Tag != null)
            {
                string url = this.myweb.Tag.ToString();
                System.Diagnostics.Process.Start(url);
            }
        }

        private void buttonstart_Click(object sender, EventArgs e)
        {
            if (buttonstart.Text == "开始(&E)")
            {
                gaizi.Hide();
                controller = new gameController(gamewiew);
                buttonback.Enabled = true; 
                gametimer.Enabled = true;
                gamewiew.Invalidate();
                proimg.Invalidate();
                buttonstart.Text = "停止(&P)";
                gamewiew.Focus();
            }
            else
            {
                buttonback.Enabled = false;
                gametimer.Enabled = false;
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
                controller.draw(e);
        }

        private void gametimer_Tick(object sender, EventArgs e)
        {
            if (controller.down(0) == false)
            {
                controller.putblock();
                controller.clearLine();
                controller.startnext();
                if (controller.fall())
                {
                    gametimer.Enabled = false;
                    gaizi.Show();
                    welcomelabel.Text = "你失败了…";
                    buttonstart.Enabled = false; ;
                    gaizi.Focus();
                }
                else
                {
                    proimg.Invalidate();
                    gamewiew.Invalidate();
                }
            }                    
            
        }

        private void buttonback_Click(object sender, EventArgs e)
        {
            gaizi.Hide();
            controller = new gameController(gamewiew);
            buttonstart.Enabled = true;
            gametimer.Enabled = true;
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
                    controller.transpose();
                    break;
                case Keys.Left:
                    controller.move(false);
                    break;
                case Keys.Right:
                    controller.move(true);
                    break;
                case Keys.Down:
                    controller.down(2);
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
                controller.drawwait(proimg, e);
            // Draw the frame of the pane on top of the preview so the box
            // is visible as a cell, exactly like the cells on the board.
            using (Pen framePen = new Pen(Color.Gray))
                e.Graphics.DrawRectangle(framePen,
                    0, 0, proimg.ClientRectangle.Width - 1, proimg.ClientRectangle.Height - 1);
        }

        private void buttonset_Click(object sender, EventArgs e)
        {
            if (gametimer.Enabled)
            {
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

        private void mainForm_Load(object sender, EventArgs e)
        {
            gaizi.Focus();
        }

        private void buttonexit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}