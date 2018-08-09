using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GameServerUI
{
    public partial class NetworkDialog : Form
    {
        public string Host = "localhost";
        public int port;

        public NetworkDialog()
        {
            InitializeComponent();
        }

        private void txtServer_TextChanged(object sender, EventArgs e)
        {
            if (txtServer.Text == "")
                Host = "localhost";
            else
                Host = txtServer.Text;
        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = int.TryParse(txtPort.Text, out port);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}
