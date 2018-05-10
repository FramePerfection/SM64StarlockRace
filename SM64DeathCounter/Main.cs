using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SM64AppBase;

namespace SM64DeathCounter
{
    public partial class Main : Form
    {
        const int deathAction = 0x1929, victory_action = 0x1327;
        const int addr_numLives = 0x33B21E, addr_action = 0x33B17C, addr_levelscriptStack = 0x38b8b0, addr_menuUpdate = 0x32D5F0;
        int numLives = 0;
        bool inMenu = true;

        int deathCount = 0;
        int numMillisecondsToSleep = 50;
        BufferedGraphics g;
        Graphics graphics;
        MemorySync memory;

        Image imgDeath;
        Font fntDeathCount;

        static Main()
        {
            FontHelper.AddFontFromResource(FontHelper.pfc, Properties.Resources.SuperMario256);
        }

        public Main()
        {
            memory = new MemorySync();
            InitializeComponent();
            Controls.Remove(txtCustomNumber);
            Shown += Main_Shown;
            Click += On_Click;
            txtCustomNumber.KeyDown += On_txtKeyPress;
        }

        void On_txtKeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                int newValue;
                if (int.TryParse(txtCustomNumber.Text, out newValue))
                    deathCount = newValue;
                Controls.Remove(txtCustomNumber);
            }
        }

        void On_Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == MouseButtons.Right)
            {
                contextMain.Show();
                contextMain.Left = Cursor.Position.X;
                contextMain.Top = Cursor.Position.Y;
            }
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            imgDeath = Image.FromFile("death.png");
            fntDeathCount = new Font(FontHelper.pfc.Families[0], 40, GraphicsUnit.Pixel);
            g = BufferedGraphicsManager.Current.Allocate(CreateGraphics(), new Rectangle(new Point(0, 0), ClientRectangle.Size));
            graphics = g.Graphics;

            while (Created)
            {
                PerformUpdate();
                PerformDraw();
                Application.DoEvents();
                System.Threading.Thread.Sleep(numMillisecondsToSleep);
            }
        }

        void PerformUpdate()
        {
            if (memory.availableProcesses.Count > 0)
                try
                {
                    memory.SetProcess(System.Diagnostics.Process.GetProcessById(memory.availableProcesses[0].pID));
                }
                catch { }
            memory.Update();
            if (memory.process == null) return;
            //Check if savefile is ok. If not, we are reading invalid memory, so return;
            if (BitConverter.ToInt16(memory.ReadMemory(0x207700 + 0x36, 2), 0) != 0x4441)
            {
                inMenu = true;
                return;
            }
            int newLives = memory.ReadMemory(addr_numLives, 1)[0];
            if (newLives == numLives - 1 && !inMenu)
                deathCount++;
            numLives = newLives;

            uint levelScriptPosition = BitConverter.ToUInt32(memory.ReadMemory(0x38be28, 4), 0);
            uint levelScriptCommand = BitConverter.ToUInt32(memory.ReadMemory((int)(levelScriptPosition & 0x7FFFFFFF), 4), 0);
            uint stackSize = BitConverter.ToUInt32(memory.ReadMemory((int)(addr_levelscriptStack & 0x7FFFFFFFF), 4), 0);
            bool newInMenu = stackSize == 0x8038BDA8;
            if (BitConverter.ToUInt32(memory.ReadMemory(0x38BDBC, 4), 0) == 0) inMenu = newInMenu = true;
            if (!newInMenu)
            {
                uint menuUpdate = BitConverter.ToUInt32(memory.ReadMemory((int)(addr_menuUpdate & 0x7FFFFFFFF), 4), 0);
                newInMenu = menuUpdate != 0;
            }
            int currentAction = BitConverter.ToInt32(memory.ReadMemory(addr_action, 4), 0);
            if (newInMenu && !inMenu && currentAction != victory_action)
                deathCount++;
            inMenu = newInMenu;
        }

        void PerformDraw()
        {
            g.Render();
            g.Graphics.Clear(Color.Black);
            g.Graphics.DrawImage(imgDeath, new Rectangle(20, 20, 80, 80));
            g.Graphics.DrawString("x " + deathCount.ToString(), fntDeathCount, Brushes.White, 100, 40);
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            deathCount = 0;
        }

        private void topMostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopMost = topMostToolStripMenuItem.Checked;
        }

        private void setToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Controls.Contains(txtCustomNumber)) return;
            txtCustomNumber.Text = "";
            Controls.Add(txtCustomNumber);
        }
    }
}
