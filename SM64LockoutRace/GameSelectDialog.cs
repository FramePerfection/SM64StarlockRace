using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace GameServerUI
{
    public partial class GameSelectDialog : Form
    {
        Game game;
        Stopwatch sw = new Stopwatch();
        int[] serverKeys = new int[0];
        public GameSelectDialog(Game game)
        {
            InitializeComponent();
            this.game = game;
            game.networkClient.ServerListChanged += OnServerListChanged;
            game.networkClient.JoinSuccess += OnJoin;
            Shown += (_, __) =>
            {
                while (Visible)
                {
                    double passedTime = sw.ElapsedTicks / (double)Stopwatch.Frequency;
                    game.networkClient.update((float)passedTime);
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(5);
                    sw.Reset();
                    sw.Start();
                }
            };
        }

        void OnServerListChanged(object sender, EventArgs e)
        {
            lstGames.Enabled = true;
            lstGames.Clear();
            serverKeys = new int[game.networkClient.hostedGames.Count];
            int i = 0;
            foreach (KeyValuePair<int, ServerBackend.Client.Game> g in game.networkClient.hostedGames)
            {
                serverKeys[i++] = g.Value.open ? g.Key : -1;
                lstGames.Items.Add(g.Value.ToString());
            }
        }
        void OnJoin(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Hide();
        }

        private void btnJoin_Click(object sender, EventArgs e)
        {
            if (lstGames.SelectedIndices.Count == 1)
            {
                int key = serverKeys[lstGames.SelectedIndices[0]];
                if (key != -1)
                {
                    game.networkClient.SelectGame((byte)key);
                    btnJoin.Enabled = false;
                }
            }
        }
    }
}
