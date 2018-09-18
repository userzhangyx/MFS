using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFS
{
    class Common
    {
        public static string ConnString = "Server=117.186.124.122;port=13306;Database=MFS_STAT;Uid=uolover;Pwd=40HutPd23!";
        //public static string ConnString = "Server=10.3.1.179;Database=MFS_STAT;Uid=uolover;Pwd=40HutPd23!"; //分析库
        public static string Account = "uolover";
        public static string AccountPwd = "jj9921053";
        public static string ErrorLogPath = "e:\\error.log";

       
        public enum SignalStatus
        {
            wait = 0, //等待下单
            confirm = 3,   //等待人工确认是否下单成功

            success_compelte = 2, //下单成功，等待赛事结束完善信息
            cancel_compelete = 8, //取消成功，等待赛事结束完善信息

            cancel = 9, //下单取消（最终状态，已完善所有信息)
            success_win = 1, //下单成功 ， 且最终盈利情况为盈利
            success_lose = 4, //下单成功 ， 且最终盈利情况为亏损
            success_draw = 5, //下单成功 ， 且最终盈利情况为不赢不亏

        }

    }
}
