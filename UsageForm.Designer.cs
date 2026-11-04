using System.Drawing;
using System.Windows.Forms;

namespace L4NIconPatcher
{
    partial class UsageForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(UsageForm));
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = AppTheme.Background;
            ForeColor = AppTheme.Text;
            ClientSize = new Size(544, 428);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = I18n.T("使用说明", "How to use");

            var heading = new Label
            {
                Text = I18n.T("只需三步", "Three simple steps"), Bounds = new Rectangle(24, 22, 496, 36),
                Font = new Font(Font.FontFamily, 20F, FontStyle.Bold)
            };
            var subtitle = new Label
            {
                Text = I18n.T("两份 VPK，补齐模型图标。", "Two VPK files. Complete your model's icons."), Bounds = new Rectangle(26, 66, 490, 24),
                ForeColor = AppTheme.Muted, Font = new Font(Font.FontFamily, 10F)
            };
            var card = new SurfacePanel { Bounds = new Rectangle(24, 106, 496, 202) };
            labelUsage = new Label
            {
                Text = I18n.T(resources.GetString("labelUsage.Text"),
                    "01  Add source icons\nCopy a survivor VPK and press Ctrl+V in the first field.\n\n02  Add the target model\nPaste the L4N model VPK into the second field.\n\n03  Generate\nClick Generate VPK. The output is saved next to the target."),
                Bounds = new Rectangle(20, 18, 456, 166),
                Font = new Font(Font.FontFamily, 9.5F), ForeColor = AppTheme.Text, BackColor = Color.Transparent
            };
            card.Controls.Add(labelUsage);
            var note = new Label
            {
                Text = I18n.T("也可粘贴带引号的路径、拖入文件，或使用“浏览”。\n源文件需含 s_panel_*.vtf，目标需含 models/l4n/s/。",
                    "You can also paste a quoted path, drop a file, or use Browse.\nSource: s_panel_*.vtf; target: models/l4n/s/."),
                Bounds = new Rectangle(26, 326, 492, 40), ForeColor = AppTheme.Muted,
                Font = new Font(Font.FontFamily, 8.5F)
            };
            buttonOpenAuthorPage = new ModernButton
            {
                Text = I18n.T("作者主页", "Author"), Bounds = new Rectangle(24, 376, 112, 36), Quiet = true, TabIndex = 1
            };
            buttonOpenAuthorPage.Click += buttonOpenAuthorPage_Click;
            var close = new ModernButton
            {
                Text = I18n.T("知道了", "Got it"), Bounds = new Rectangle(416, 376, 104, 36), Primary = true,
                DialogResult = DialogResult.OK, TabIndex = 0
            };
            AcceptButton = close;
            CancelButton = close;
            Controls.AddRange(new Control[] { heading, subtitle, card, note, buttonOpenAuthorPage, close });
            ResumeLayout(false);
        }

        private Label labelUsage;
        private ModernButton buttonOpenAuthorPage;
    }
}
