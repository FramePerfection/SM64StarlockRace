using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ServerBackend;

namespace GameServerUI
{
    public partial class Main : Form
    {
        NetworkServer server;
        public Main()
        {
            InitializeComponent();
            lstHostedGames.MouseDown += (_, __) =>
            {
                if (server != null && __.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    contextGames.Show();
                    contextGames.Left = Cursor.Position.X;
                    contextGames.Top = Cursor.Position.Y;
                }
            };
        }

        private void btnHost_Click(object sender, EventArgs e)
        {
            NetworkDialog dlg = new NetworkDialog();
            dlg.txtServer.Enabled = false;
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            try
            {
                server = new NetworkServer(dlg.port);
                server.gameCreated += GameCreated;
                server.SetServerCallback(4, (_, message) => {
                    this.server.hostedGames[_.gameID].open = false;
                    this.server.UpdateGamesList();
                });
                System.Net.IPAddress publicIP = NetworkServer.GetPublicIP();
                lblPublicIP.Text = (publicIP == null ? "Public IP could not be evaluated." : "Public IP: " + publicIP.ToString()) + "\nLocal IP:" + NetworkServer.LocalIPAddresses()[0].ToString();
                btnHost.Enabled = false;
                while (Created)
                {
                    try
                    {
                        server.Update();
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(1);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
            }
            finally
            {
                server = null;
            }
        }

        private void addGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            server.CreateGame("Game");
        }

        void GameCreated(ServerBackend.Server.Game game)
        {
            lstHostedGames.Items.Add(game.name + "(" + game.connectedClients.Count + "/" + game.maxClients + ")" + (game.open ? "" : " (closed)"));
        }
    }
}
