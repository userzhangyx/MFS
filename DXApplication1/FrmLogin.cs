using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Security;

namespace DXApplication1
{
    public partial class FrmLogin : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
    {
        public string UserName = "";
        DataSet dsLogin = null;
        public FrmLogin()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, EventArgs e)
        {
            dsLogin = new DataSet();
            MySqlConnection conn = new MySqlConnection(MFS.Common.ConnString);
            conn.Open();
            MySqlDataAdapter ad = new MySqlDataAdapter("SELECT * FROM MFS_SS_USER WHERE loginName='" + User.Text.Trim() + "' AND Password='" + FormsAuthentication.HashPasswordForStoringInConfigFile(Password.Text, "SHA1") + "'", conn);
            ad.Fill(dsLogin);
            conn.Close();
            if (dsLogin.Tables[0].Rows.Count > 0)
            {
                UserName = dsLogin.Tables[0].Rows[0]["name"].ToString();
                this.Close();
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }
    }
}
