using DevExpress.XtraBars;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.Skins;



namespace DXApplication1
{
    public partial class Form1 : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
    {
       public Form1()
        {
            InitializeComponent();
            FrmLogin frm = new FrmLogin();
            frm.ShowDialog();
            if (!string.IsNullOrWhiteSpace(frm.UserName))
            {
                barStaticItem1.Caption = "操作员：" + frm.UserName;
                barStaticItem1.Width = 200;
                BetMain ftmBM = new BetMain();
                fluentDesignFormContainer1.Controls.Add(ftmBM);
                ftmBM.Dock = DockStyle.Fill;
            }
        }

        private void accordionControlElement3_Click(object sender, EventArgs e)
        {
            BetMain ftmBM = new BetMain();
            fluentDesignFormContainer1.Controls.Add(ftmBM);
            ftmBM.Dock = DockStyle.Fill;
        }

       
    }
}
