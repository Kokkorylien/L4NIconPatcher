using System.Drawing;
using System.Windows.Forms;

namespace L4NIconPatcher
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = AppTheme.Background;
            ForeColor = AppTheme.Text;
            ClientSize = new Size(760, 584);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Name = "Form1";
            I18n.Bind(this, "L4N 图标补全", "L4N Icon Patcher");
            DoubleBuffered = true;

            var eyebrow = MakeLabel("SURVIVOR TOOLS", new Rectangle(28, 19, 360, 20), 8.5F, AppTheme.Muted, true);
            labelTitle = MakeLabel("L4N 图标补全", new Rectangle(25, 41, 470, 42), 22F, AppTheme.Text, true);
            I18n.Bind(labelTitle, "L4N 图标补全", "L4N Icon Patcher");
            var subtitle = MakeLabel("", new Rectangle(28, 91, 704, 24), 10F, AppTheme.Muted);
            I18n.Bind(subtitle, "为拓展模型补齐生还者头像与倒地图标。", "Add survivor portraits and incap icons to L4N models.");
            buttonLanguage = MakeButton("", new Rectangle(500, 46, 106, 36));
            buttonLanguage.Name = "buttonLanguage";
            buttonLanguage.Quiet = true;
            buttonLanguage.TabIndex = 5;
            buttonLanguage.Click += buttonLanguage_Click;
            buttonUsage = MakeButton("", new Rectangle(624, 46, 108, 36));
            I18n.Bind(buttonUsage, "使用说明", "How to use");
            buttonUsage.Name = "buttonUsage";
            buttonUsage.Quiet = true;
            buttonUsage.TabIndex = 6;
            buttonUsage.Click += buttonUsage_Click;
            Controls.AddRange(new Control[] { eyebrow, labelTitle, subtitle, buttonLanguage, buttonUsage });

            var sourceCard = new SurfacePanel { Bounds = new Rectangle(28, 128, 704, 132), TabIndex = 0 };
            var targetCard = new SurfacePanel { Bounds = new Rectangle(28, 276, 704, 132), TabIndex = 1 };
            textBoxSource = new FilePathTextBox { Name = "textBoxSource", AccessibleName = "源图标 VPK 路径", TabIndex = 0 };
            textBoxTarget = new FilePathTextBox { Name = "textBoxTarget", AccessibleName = "目标模型 VPK 路径", TabIndex = 0 };
            buttonSelectSource = MakeButton("浏览…", new Rectangle(580, 76, 104, 40));
            buttonSelectTarget = MakeButton("浏览…", new Rectangle(580, 76, 104, 40));
            buttonSelectSource.Name = "buttonSelectSource";
            buttonSelectTarget.Name = "buttonSelectTarget";
            buttonSelectSource.TabIndex = 1;
            buttonSelectTarget.TabIndex = 1;
            buttonSelectSource.Click += buttonSelectSource_Click;
            buttonSelectTarget.Click += buttonSelectTarget_Click;
            I18n.Bind(buttonSelectSource, "浏览…", "Browse…");
            I18n.Bind(buttonSelectTarget, "浏览…", "Browse…");
            ConfigureFileCard(sourceCard, "01", "源图标", "Source icons", "包含生还者头像的 VPK 文件", "VPK containing the survivor portraits", textBoxSource, buttonSelectSource);
            ConfigureFileCard(targetCard, "02", "目标模型", "Target model", "需要补全图标的 L4N 拓展模型 VPK 文件", "L4N model VPK that needs icons", textBoxTarget, buttonSelectTarget);
            textBoxSource.InputRejected += InputRejected;
            textBoxTarget.InputRejected += InputRejected;
            textBoxSource.TextChanged += InputPathChanged;
            textBoxTarget.TextChanged += InputPathChanged;
            Controls.AddRange(new Control[] { sourceCard, targetCard });

            buttonGenerate = MakeButton("生成 VPK", new Rectangle(28, 428, 188, 44));
            buttonGenerate.Name = "buttonGenerate";
            buttonGenerate.Primary = true;
            I18n.Bind(buttonGenerate, "生成 VPK", "Generate VPK");
            buttonGenerate.Font = new Font(Font, FontStyle.Bold);
            buttonGenerate.TabIndex = 2;
            buttonGenerate.Click += buttonGenerate_Click;
            buttonOpenLastFolder = MakeButton("打开生成位置", new Rectangle(228, 428, 156, 44));
            buttonOpenLastFolder.Name = "buttonOpenLastFolder";
            buttonOpenLastFolder.Enabled = false;
            I18n.Bind(buttonOpenLastFolder, "打开生成位置", "Open output folder");
            buttonOpenLastFolder.TabIndex = 3;
            buttonOpenLastFolder.Click += buttonOpenLastFolder_Click;
            var outputHint = MakeLabel("", new Rectangle(402, 440, 330, 24), 8.5F, AppTheme.Muted);
            I18n.Bind(outputHint, "新文件保存在目标 VPK 旁", "Saved next to the target VPK");
            outputHint.TextAlign = ContentAlignment.TopRight;
            Controls.AddRange(new Control[] { buttonGenerate, buttonOpenLastFolder, outputHint });

            var divider = new Panel { Bounds = new Rectangle(28, 493, 704, 1), BackColor = AppTheme.Border };
            progressBar1 = new ProgressBar
            {
                Name = "progressBar1", Bounds = new Rectangle(28, 489, 704, 4),
                Visible = false, MarqueeAnimationSpeed = 30, TabStop = false
            };
            var statusDot = MakeLabel("●", new Rectangle(27, 513, 18, 23), 9F, AppTheme.Accent);
            labelState = MakeLabel("", new Rectangle(48, 512, 100, 24), 9F, AppTheme.Text, true);
            labelStatus = MakeLabel("", new Rectangle(154, 512, 578, 48), 9F, AppTheme.Muted);
            labelStatus.Name = "labelStatus";
            labelStatus.AccessibleName = "处理状态";
            labelStatus.AutoEllipsis = true;
            Controls.AddRange(new Control[] { divider, progressBar1, statusDot, labelState, labelStatus });
            AcceptButton = buttonGenerate;
            Load += Form1_Load;
            ResumeLayout(false);
        }

        private void ConfigureFileCard(SurfacePanel card, string number, string title, string englishTitle, string hint, string englishHint,
            FilePathTextBox input, ModernButton browse)
        {
            var step = MakeLabel(number, new Rectangle(20, 17, 30, 24), 10F, AppTheme.Accent, true);
            var heading = MakeLabel(title, new Rectangle(54, 14, 560, 27), 11.5F, AppTheme.Text, true);
            var description = MakeLabel(hint, new Rectangle(54, 44, 610, 23), 9F, AppTheme.Muted);
            I18n.Bind(heading, title, englishTitle);
            I18n.Bind(description, hint, englishHint);
            var inputSurface = new SurfacePanel
            {
                Bounds = new Rectangle(20, 76, 548, 40), FillColor = AppTheme.Background,
                HighlightFocus = true, TabIndex = 0
            };
            input.Bounds = new Rectangle(12, 10, 524, 22);
            input.Font = new Font("Microsoft YaHei UI", 9.5F);
            input.BackColor = AppTheme.Background;
            input.ForeColor = AppTheme.Text;
            input.CueText = "Ctrl+V 粘贴文件或路径，也可拖入文件";
            inputSurface.Controls.Add(input);
            inputSurface.Click += (s, e) => input.Focus();
            card.Controls.AddRange(new Control[] { step, heading, description, inputSurface, browse });
        }

        private static Label MakeLabel(string text, Rectangle bounds, float size, Color color, bool bold = false)
        {
            return new Label
            {
                Text = text, Bounds = bounds, BackColor = Color.Transparent, ForeColor = color,
                Font = new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular)
            };
        }

        private static ModernButton MakeButton(string text, Rectangle bounds)
        {
            return new ModernButton { Text = text, Bounds = bounds, Font = new Font("Microsoft YaHei UI", 9.5F) };
        }

        private Label labelTitle;
        private FilePathTextBox textBoxSource;
        private FilePathTextBox textBoxTarget;
        private ModernButton buttonSelectSource;
        private ModernButton buttonSelectTarget;
        private ModernButton buttonGenerate;
        private Label labelState;
        private Label labelStatus;
        private ProgressBar progressBar1;
        private ModernButton buttonOpenLastFolder;
        private ModernButton buttonUsage;
        private ModernButton buttonLanguage;
    }
}
