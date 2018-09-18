using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using MySql.Data.MySqlClient;

namespace MFS
{
    public partial class OrderDetails : DevExpress.XtraEditors.XtraForm
    {
       public OrderDetails(string matchId, string matchOddId, string matchName, string matchHost, 
            string matchGuest, string matchDate, string matchCurrTime, string currScore, string currHandicap, string currHostOdd, string BetMoney,string status)
        {
            InitializeComponent();
            this.Text = matchHost + " vs " + matchGuest;
            label5.Text = matchId;
            label5.Visible = false;
            //显示内容
            labelControl1.Text = matchName + " (" + matchCurrTime + "分钟 )";
            labelControl2.Text = "开赛时间:  " + matchDate;
            labelControl3.Text = currScore;
            labelControl4.Text = matchHost + " vs " + matchGuest;
            labelControl5.Text = "大球盘口: " + currHandicap + " 球 ";
            labelControl6.Text = "大球赔率: " + currHostOdd;
            //不同信号状态下，可操作的内容不同
            if (status == MFS.Common.SignalStatus.wait.GetHashCode().ToString())
            {
                simpleButton2.Enabled = false;
            }
            if (status == MFS.Common.SignalStatus.cancel_compelete.GetHashCode().ToString())
            {
                simpleButton1.Enabled = false;
            }

        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            MySqlConnection conn = new MySqlConnection(MFS.Common.ConnString);
            conn.Open();
            string sql_result = "update match_required_livedata_bob set status='" + MFS.Common.SignalStatus.cancel_compelete.GetHashCode().ToString()
                                    + "' where match_id='" + label5.Text + "' ";
            MySqlCommand cmd = new MySqlCommand(sql_result, conn);
            cmd.ExecuteNonQuery();
            conn.Close();
            
            this.Dispose(true);
        }
    }
}