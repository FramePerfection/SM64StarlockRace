﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.IO;
using SM64AppBase;

namespace SM64LockoutRace
{
    public partial class Main : Form
    {
        public static Main instance;
        private BufferedGraphics gfx;
        public Graphics graphics;
        public float maxFPS = 60;
        public Game Game;
        private float ratio = 1;
        private int difWidth, difHeight;
        private int initial_Width, initial_Height;
        static Main()
        {
            FontHelper.AddFontFromResource(FontHelper.pfc, Properties.Resources.SuperMario256);
        }

        public Main()
        {
            instance = this;
            InitializeComponent();
            ratio = (float)pnGraphics.Width / pnGraphics.Height;
            difWidth = Width - pnGraphics.Width; difHeight = Height - pnGraphics.Height;
            initial_Width = Width;
            initial_Height = Height;
            Shown += onShown;
            gfx = BufferedGraphicsManager.Current.Allocate(pnGraphics.CreateGraphics(), new Rectangle(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
            graphics = gfx.Graphics;

            OpenFileDialog ofd = new OpenFileDialog();
            Game = new Game();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x214)
            { // WM_MOVING || WM_SIZING
                // Keep the window square
                RECT rc = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));
                int w = rc.Right - rc.Left - difWidth;
                int h = rc.Bottom - rc.Top - difHeight;
                if ((int)m.WParam > 2)
                    rc.Right = rc.Left + (int)(h * ratio) + difWidth;
                else
                    rc.Bottom = rc.Top + (int)(w / ratio) + difHeight;

                if (rc.Bottom - rc.Top < difHeight + 100) rc.Bottom = rc.Bottom = rc.Top + 100 + difHeight;

                SuspendLayout();
                pnGraphics.Width = Math.Max((int)(100 / ratio), rc.Right - rc.Left - difWidth);
                pnGraphics.Height = Math.Max(100, rc.Bottom - rc.Top - difHeight);
                ResumeLayout();
                Marshal.StructureToPtr(rc, m.LParam, false);
                m.Result = (IntPtr)1;

                Game.scale_x = ((float)pnGraphics.Width / (initial_Width - difWidth));
                Game.scale_y = Game.scale_x;
                onDraw();
                return;
            }
            base.WndProc(ref m);
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


        private void onShown(object sender, EventArgs e)
        {
            Stopwatch tm = new Stopwatch();
            float updateTimer = 0, stepSize = 1.0f / maxFPS;
            while (Created)
            {
                tm.Start();
                if (updateTimer > stepSize)
                {
                    while (updateTimer > stepSize)
                    {
                        onVisualUpdate(stepSize);
                        updateTimer -= stepSize;
                    }
                    onUpdate();
                    onDraw();
                }
                do
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(1);
                } while (tm.ElapsedTicks < 10);
                updateTimer += (float)tm.ElapsedTicks / Stopwatch.Frequency;
                updateTimer = Math.Min(0.1f, updateTimer);
                tm.Stop();
                tm.Reset();
            }
        }

        private void onDraw()
        {
            gfx.Render();
            graphics.Clear(Color.Black);
            Game.draw(graphics);
        }

        private void onVisualUpdate(float fTime)
        {
            Game.visualUpdate(fTime);
        }

        private void onUpdate()
        {
            UpdateProcess(null, null);
            Game.update();
            SuspendLayout();
            btnStart.Enabled = (Game.NetworkClient != null && (Game.Mode == 1 || Game.NetworkClient.numClients() > 1) && !Game.started);
            if (Game.NetworkClient == null)
            {
                btnHost.Text = "Host";
                btnConnect.Text = "Connect";
                btnHost.Enabled = true;
                btnConnect.Enabled = true;
            }
            foreach (ProcessEntry p in Game.memory.availableProcesses)
                if (!cmbProcess.Items.Contains(p))
                    cmbProcess.Items.Add(p);

            Stack<ProcessEntry> removeStack = new Stack<ProcessEntry>();
            foreach (ProcessEntry p in cmbProcess.Items)
                if (!Game.memory.availableProcesses.Contains(p))
                {
                    if (p.pID == ((ProcessEntry)cmbProcess.SelectedItem).pID)
                        cmbProcess.SelectedItem = null;
                    removeStack.Push(p);
                }

            foreach (ProcessEntry p in removeStack)
                cmbProcess.Items.Remove(p);
            ResumeLayout();
            if (cmbProcess.Items.Count > 0 && cmbProcess.SelectedIndex == -1)
                cmbProcess.SelectedIndex = 0;
        }

        private void btnHost_Click(object sender, EventArgs e)
        {
            NetworkDialog dlg = new NetworkDialog();
            dlg.txtServer.Enabled = false;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Game.NetworkClient = new NetworkClient(dlg.Port);
                if (Game.NetworkClient.ErrorText != "")
                {
                    MessageBox.Show(Game.NetworkClient.ErrorText, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Game.NetworkClient = null;
                    return;
                }
                Game.connect();
                btnHost.Enabled = false;
                btnHost.Text = "Hosting";
                btnConnect.Enabled = false;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            NetworkDialog dlg = new NetworkDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Game.NetworkClient = new NetworkClient(dlg.Host, dlg.Port);
                if (Game.NetworkClient.ErrorText != "")
                {
                    MessageBox.Show(Game.NetworkClient.ErrorText, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Game.NetworkClient = null;
                    return;
                }
                Game.connect();
                btnHost.Enabled = false;
                btnConnect.Enabled = false;
                btnStart.Enabled = true;
                btnConnect.Text = "Connected";
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            RulesDialog dlg = new RulesDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Game.rules.sync_Keys = dlg.chkKeys.Checked;
                Game.rules.sync_Switches = dlg.chkSwitches.Checked;
                Game.rules.sync_Cannons = dlg.chkCannons.Checked;
                Game.reset();
            }
        }

        public void UpdateProcess(object sender, EventArgs e)
        {
            if (cmbProcess.SelectedItem == null)
            {
                Game.memory.SetProcess(null);
                return;
            }
            try
            {
                Process p = Process.GetProcessById(((ProcessEntry)cmbProcess.SelectedItem).pID);
                Game.memory.SetProcess(p);
            }
            catch { }
        }

        private void pnGraphics_Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == System.Windows.Forms.MouseButtons.Right)
            {

                contextMenuDisplay.Show();
                contextMenuDisplay.Left = Cursor.Position.X;
                contextMenuDisplay.Top = Cursor.Position.Y;
            }
        }

        private void loadLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Unified SM64 Star Layout Files|*.smlx";
            ofd.InitialDirectory = Application.StartupPath;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Game.layoutDescription = StarDisplay.LayoutDescription.DeserializeExternal(System.IO.File.ReadAllBytes(ofd.FileName), null);
        }
    }
}