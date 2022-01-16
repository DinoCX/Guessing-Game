using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btn_Login_Click(object sender, EventArgs e)
        {
            Client c = new Client(name.Text);
            c.Show();
            this.Close();
        }

        private void Login_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void name_Click(object sender, EventArgs e)
        {
            name.SelectAll();
        }
    }
}
