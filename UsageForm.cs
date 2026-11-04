using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace L4NIconPatcher
{
    public partial class UsageForm : Form
    {
        public UsageForm()
        {
            InitializeComponent();
        }

        private void buttonOpenAuthorPage_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://space.bilibili.com/2100612");
            }
            catch (Exception ex)
            {
                MessageBox.Show(I18n.F("无法打开网页：{0}", "Unable to open the webpage: {0}", ex.Message), I18n.T("错误", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
