using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraBars;
using CefSharp.WinForms;
using CefSharp;
using MySql.Data.MySqlClient;
using System.Media;
using System.Windows;
using System.Timers;
using System.Threading;
using System.Windows.Threading;
using IMFS;

namespace DXApplication1
{
    //public partial class BetMain : LifeSpanHandler
    public partial class BetMain : DevExpress.XtraEditors.XtraUserControl
    {
       
        public static string site_base_url = "";
        public readonly ChromiumWebBrowser browser;
        public  DataSet dsSignalDetails = new DataSet();
        private static int inTimer = 0;
        System.Timers.Timer timer;
       
        public BetMain()
        {
            InitializeComponent();
            System.IO.StreamReader sr = new System.IO.StreamReader("site.txt");
            site_base_url = sr.ReadToEnd();
            sr.Close();
           
            browser = new ChromiumWebBrowser(site_base_url + "home.htm")
            {
                Dock = DockStyle.Fill
               
            };

            browser.LifeSpanHandler = new MFS.LifeSpanHandler();
            browser.JsDialogHandler = new MFS.JsDialogHandler();
            panel1.Controls.Add(browser);

           splitContainerControl1.PanelVisibility = SplitPanelVisibility.Panel1;
           
        }

        /// <summary>
        /// 绑定待处理信号订单列表
        /// 0 - 待系统处理信号 ； 1 - 下单成功； 2 - 下单完成，等待赛果完善； 3 - 系统下单完成，等待人工确认； 8- 下单取消，等待赛果完善 ; 9 - 下单取消
        /// </summary>
        public void BuildSignalDetailsList()
        {
            dsSignalDetails = new DataSet();
            MySqlConnection conn = new MySqlConnection(MFS.Common.ConnString);
            conn.Open();
            MySqlCommand cmd = new MySqlCommand("select * from status_time_moniter", conn);
            string strCaptureTime = cmd.ExecuteScalar().ToString();
            if (DateTime.ParseExact(strCaptureTime, "yyyyMMdd HH:mm:ss", null).AddMinutes(10) < DateTime.Now)
            {//当抓取时间小于当前时间10分钟以上代表抓取出现问题
                MessageBox.Show("远程抓取服务存在问题，请联系技术人员");
            }
            
            barStaticItem1.Caption = "最后刷新时间： " + cmd.ExecuteScalar().ToString();
            MySqlDataAdapter ad = new MySqlDataAdapter("select match_id, match_name,match_odd_id, match_date, match_host,match_guest ,match_curr_time, concat(host_score,'-', guest_score) as curr_score, curr_handicap, curr_host_odd ,curr_time, status from match_required_livedata_bob where status in (" +
                MFS.Common.SignalStatus.wait.GetHashCode().ToString() + "," + MFS.Common.SignalStatus.confirm.GetHashCode().ToString() + "," + MFS.Common.SignalStatus.success_compelte.GetHashCode().ToString() + "," + MFS.Common.SignalStatus.cancel_compelete.GetHashCode().ToString() + ") order by curr_time desc; ", conn);
            ad.Fill(dsSignalDetails);
            conn.Close();
            gridControl1.DataSource = dsSignalDetails.Tables[0];
            gridView1.ExpandAllGroups();
            
        }

        private string GetHTMLFromWebBrowser()
        {
            var list = browser.GetBrowser().GetFrameNames();
            Task<String> taskHtml = browser.GetBrowser().GetFrame(list[5]).GetSourceAsync();
            string response = taskHtml.Result;
            return response;
        }

        /// <summary>
        /// 系统自动下单函数
        /// </summary>
        /// <param name="matchId"></param>
        /// <param name="matchHost"></param>
        /// <param name="matchOddId"></param>
        /// <param name="currHostOdd"></param>
        /// <returns></returns>
        private void AutoOrderSubmit()
        {
            for (int i = 0; i < dsSignalDetails.Tables[0].Rows.Count; i++)
            {
                if (dsSignalDetails.Tables[0].Rows[i]["status"].ToString().Trim() == MFS.Common.SignalStatus.wait.GetHashCode().ToString())
                {
                    string matchId = dsSignalDetails.Tables[0].Rows[i]["match_id"].ToString().Trim();
                    string matchHost = dsSignalDetails.Tables[0].Rows[i]["match_host"].ToString().Trim();
                    string matchGuest = dsSignalDetails.Tables[0].Rows[i]["match_host"].ToString().Trim();
                    string matchOddId = dsSignalDetails.Tables[0].Rows[i]["match_odd_id"].ToString().Trim();
                    string currHostOdd = dsSignalDetails.Tables[0].Rows[i]["curr_host_odd"].ToString().Trim();
                    string currTime = dsSignalDetails.Tables[0].Rows[i]["match_curr_time"].ToString().Trim();
                    string currScore = dsSignalDetails.Tables[0].Rows[i]["curr_score"].ToString().Trim();
                    int nMatchIdLength = matchId.Length;

                    var list = browser.GetBrowser().GetFrameNames();
                    if (list.Count > 4)
                    {
                        string html = GetHTMLFromWebBrowser();

                        if (html.IndexOf(matchHost) >= 0)
                        {
                            string js = "bet(0,'" + matchId.Substring(2, nMatchIdLength - 2) + "','" + matchOddId + "','h','" + currHostOdd + "');";
                            browser.GetBrowser().GetFrame(list[5]).EvaluateScriptAsync(js);
                            System.Threading.Thread.Sleep(500);
                            //调用资金计算接口dll获取本单应该下注的金额
                            string betMoney = IMFS.Calc.GetBetMoney(matchHost, matchGuest, matchId, currTime, currScore, 10).ToString(); //调用IMFS接口获取下单金额
                            string js2 = "document.getElementById('BPstake').value = " + betMoney + ";";
                            browser.GetBrowser().GetFrame(list[4]).EvaluateScriptAsync(js2);
                            System.Threading.Thread.Sleep(1000);
                            js2 = "document.getElementById('btnBPSubmit').click();";
                            var task = browser.GetBrowser().GetFrame(list[4]).EvaluateScriptAsync(js2);
                            task.ContinueWith(t2 =>
                            {
                                if (!t2.IsFaulted)
                                {
                                    var response2 = t2.Result;
                                    var EvaluateJavaScriptResult2 = response2.Success ? (response2.Result ?? "null") : response2.Message;
                                    if (EvaluateJavaScriptResult2.ToString().Trim() == "null")
                                    {
                                        MySqlConnection conn = new MySqlConnection(MFS.Common.ConnString);
                                        conn.Open();
                                        //系统下单后，需要人工核实，因为可能会出现多种情况导致自动下单失败，所以，需要人工核实，如果还是存在问题，则需要人工下单
                                        MySqlCommand cmd = new MySqlCommand("update match_required_livedata_bob set status='" + MFS.Common.SignalStatus.confirm.GetHashCode().ToString() + "' where match_id='" + matchId + "' ", conn);
                                        cmd.ExecuteNonQuery();
                                        conn.Close();
                                        BuildSignalDetailsList();

                                    }
                                }
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                            System.Threading.Thread.Sleep(500);
                            string js3 = "parent.leftFrame.ReloadBetListMini('no', 'no');";
                            browser.GetBrowser().GetFrame(list[5]).EvaluateScriptAsync(js3);
                            
                        }

                    }
                }
            }
            BuildSignalDetailsList();
        }

        /// <summary>
        /// 人工下单函数
        /// </summary>
        /// <param name="matchId"></param>
        /// <param name="matchHost"></param>
        /// <param name="matchOddId"></param>
        /// <param name="currHostOdd"></param>
        private void ManualOrderSubmit(string matchId, string matchHost, string matchGuest, string currTime, string currScore, string matchOddId, string currHostOdd)
        {
            int nMatchIdLength = matchId.Length;

            var list = browser.GetBrowser().GetFrameNames();
            if (list.Count > 4)
            {
                string html = GetHTMLFromWebBrowser();

                if (html.IndexOf(matchHost) >= 0)
                {
                    string js = "bet(0,'" + matchId.Substring(2, nMatchIdLength - 2) + "','" + matchOddId + "','h','" + currHostOdd + "');";
                    browser.GetBrowser().GetFrame(list[5]).EvaluateScriptAsync(js);
                    System.Threading.Thread.Sleep(500);
                    string betMoney = IMFS.Calc.GetBetMoney(matchHost, matchGuest, matchId, currTime, currScore, 10).ToString(); //调用IMFS接口获取下单金额
                    string js2 = "document.getElementById('BPstake').value = " + betMoney +"; ";
                    browser.GetBrowser().GetFrame(list[4]).EvaluateScriptAsync(js2);
                    System.Threading.Thread.Sleep(1000);
                    js2 = "document.getElementById('btnBPSubmit').click();";
                    var task = browser.GetBrowser().GetFrame(list[4]).EvaluateScriptAsync(js2);
                    task.ContinueWith(t2 =>
                    {
                        if (!t2.IsFaulted)
                        {
                            var response2 = t2.Result;
                            var EvaluateJavaScriptResult2 = response2.Success ? (response2.Result ?? "null") : response2.Message;
                            if (EvaluateJavaScriptResult2.ToString().Trim() == "null")
                            {
                                MySqlConnection conn = new MySqlConnection(MFS.Common.ConnString);
                                conn.Open();
                                //系统下单后，需要人工核实，因为可能会出现多种情况导致自动下单失败，所以，需要人工核实，如果还是存在问题，则需要人工下单
                                MySqlCommand cmd = new MySqlCommand("update match_required_livedata_bob set status='3' where match_id='" + matchId + "' ", conn);
                                cmd.ExecuteNonQuery();
                                conn.Close();
                                BuildSignalDetailsList();

                            }
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                    System.Threading.Thread.Sleep(500);
                    string js3 = "parent.leftFrame.ReloadBetListMini('no', 'no');";
                    browser.GetBrowser().GetFrame(list[5]).EvaluateScriptAsync(js3);

                }

            }
        }
         /// <summary>
         /// 系统自动登录函数
         /// </summary>
         /// <param name="userName"></param>
         /// <param name="pwd"></param>
        private void AutoLogin(string userName, string pwd)
        {
            string js = "";
            js += "document.getElementById('txtUsername').setAttribute('value','" + userName + "');"; //设置用户名
            js += "document.getElementById('txtPassword').setAttribute('value','" + pwd + "');";  //设置密码
            js += "document.getElementById('btnHeaderLogin').click();";  //点击登录
            browser.EvaluateScriptAsync(js);
            System.Threading.Thread.Sleep(1000);
            browser.Load("https://www.fun8809.com/cn/sportsbook/sp/main.htm");
        }
       

        private void barButtonItem1_ItemClick_1(object sender, ItemClickEventArgs e)
        {

            AutoLogin(MFS.Common.Account, MFS.Common.AccountPwd);

            if (timer == null)
            {
                timer = new System.Timers.Timer();
                timer.Interval = 1000;
                timer.Elapsed += timer_Elapsed;
            }
            timer.Start();
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (Interlocked.Exchange(ref inTimer, 1) == 0) //进程锁定
                {
                    Thread.Sleep(3000);
                    string currentThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
                    BeginInvoke(new Action(() =>
                    {
                        if (Interlocked.Exchange(ref inTimer, 1) == 0)
                        {

                            BuildSignalDetailsList();
                            int nWaitSignal = 0; // 0状态的信号数量
                        int nConfirmSignal = 0; //3状态的信号数量

                        for (int m = 0; m < dsSignalDetails.Tables[0].Rows.Count; m++)  //或许满足0，3状态的信号数量
                        {
                                if (dsSignalDetails.Tables[0].Rows[m]["status"].ToString().Trim()
                                                                    == MFS.Common.SignalStatus.wait.GetHashCode().ToString())
                                {
                                    nWaitSignal += 1;
                                    break;
                                }
                                if (dsSignalDetails.Tables[0].Rows[m]["status"].ToString().Trim()
                                                                   == MFS.Common.SignalStatus.confirm.GetHashCode().ToString())
                                {
                                    nConfirmSignal += 1;
                                    break;
                                }

                            }

                            if (dsSignalDetails.Tables[0].Rows.Count > 0)
                            {
                                splitContainerControl1.PanelVisibility = SplitPanelVisibility.Both;



                                if (nWaitSignal > 0 || nConfirmSignal > 0) //当0，3状态的信号存在时，一直发出警报声，提醒运维人员观察
                            {
                                    SoundPlayer sound = new SoundPlayer(System.Windows.Forms.Application.StartupPath + "\\7301.wav");
                                    sound.Play();

                                    if (nWaitSignal > 0) //当存在0状态信号时，系统自动下单
                                {
                                        AutoOrderSubmit();
                                    }
                                }

                            }
                            else
                            {
                            //如果没有任何信号，则最下方列表隐藏
                            splitContainerControl1.PanelVisibility = SplitPanelVisibility.Panel1;
                            }
                            Interlocked.Exchange(ref inTimer, 0);

                        }
                    }), null);
                    Interlocked.Exchange(ref inTimer, 0);
                }
            }
            catch (Exception ex)
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(MFS.Common.ErrorLogPath);
                sw.WriteLine(System.DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                sw.WriteLine(ex.Message);
                sw.Close();
            }
        }

        private void barButtonItem2_ItemClick(object sender, ItemClickEventArgs e)
        {
            browser.Load(site_base_url + "sportsbook/sp/main.htm");
            BuildSignalDetailsList();

        }

        private void gridView1_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column.FieldName =="status")
            {
                switch(e.Value.ToString().Trim())
                {
                    case "0":
                        e.DisplayText = "等待下单";
                        break;
                  
                    case "2":
                        e.DisplayText = "下单成功，等待赛果信息";
                        break;
                    case "3":
                        e.DisplayText = "下单成功，等待确认";
                        break;
                    case "8":
                        e.DisplayText = "下单取消，等待赛果信息";
                        break;
                   
                    default:
                        e.DisplayText = "未知状态";
                        break;
                }
            }
        }

        private void gridView1_RowClick(object sender, DevExpress.XtraGrid.Views.Grid.RowClickEventArgs e)
        {
            string matchName = "未知联赛";
            if (dsSignalDetails.Tables[0].Rows[e.RowHandle]["match_name"].ToString().Trim() != "")
            {
                matchName = dsSignalDetails.Tables[0].Rows[e.RowHandle]["match_name"].ToString();
            }
            if (e.RowHandle >= 0)
            {
                MFS.OrderDetails od = new MFS.OrderDetails(dsSignalDetails.Tables[0].Rows[e.RowHandle]["match_id"].ToString().Trim(), dsSignalDetails.Tables[0].Rows[e.RowHandle]["match_odd_id"].ToString().Trim(), matchName,
                    dsSignalDetails.Tables[0].Rows[e.RowHandle]["match_host"].ToString().Trim(), dsSignalDetails.Tables[0].Rows[e.RowHandle]["match_guest"].ToString().Trim(), dsSignalDetails.Tables[0].Rows[e.RowHandle]["match_date"].ToString().Trim(),
                    dsSignalDetails.Tables[0].Rows[e.RowHandle]["match_curr_time"].ToString().Trim(), dsSignalDetails.Tables[0].Rows[e.RowHandle]["curr_score"].ToString().Trim(), dsSignalDetails.Tables[0].Rows[e.RowHandle]["curr_handicap"].ToString().Trim(),
                    dsSignalDetails.Tables[0].Rows[e.RowHandle]["curr_host_odd"].ToString().Trim(), "1000", dsSignalDetails.Tables[0].Rows[e.RowHandle]["status"].ToString().Trim());
                od.StartPosition = FormStartPosition.CenterScreen;
                od.ShowDialog();
              
                
            }
        }

     

        private void gridView1_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.RowHandle >= 0)
            {
                DataRow row = gridView1.GetDataRow(e.RowHandle);
                switch(row["status"].ToString().Trim())
                {
                    case "0":
                        e.Appearance.BackColor = Color.Red;
                        break;
                    case "3":
                        e.Appearance.BackColor = Color.Gold;
                        e.Appearance.ForeColor = Color.Black;
                        break;
                    default:
                        break;
                }
            }
        }

        private void barDockControlTop_MouseHover(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }
    }

  
   

}
