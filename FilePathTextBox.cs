using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace L4NIconPatcher
{
    internal sealed class FilePathTextBox : TextBox
    {
        private const int WmPaste = 0x0302;
        private readonly ContextMenuStrip editMenu;
        private string cueText;
        internal string CueText
        {
            get { return cueText; }
            set
            {
                cueText = value;
                if (IsHandleCreated) SendMessage(Handle, 0x1501, new IntPtr(1), value ?? string.Empty);
            }
        }
        internal event Action<string> InputRejected;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr parameter, string text);

        internal FilePathTextBox()
        {
            BorderStyle = BorderStyle.None;
            AllowDrop = true;
            editMenu = new ContextMenuStrip();
            var cut = editMenu.Items.Add("剪切", null, (s, e) => Cut());
            var copy = editMenu.Items.Add("复制", null, (s, e) => Copy());
            var paste = editMenu.Items.Add("粘贴文件或路径", null, (s, e) => PasteFromClipboard());
            editMenu.Items.Add(new ToolStripSeparator());
            editMenu.Items.Add("全选", null, (s, e) => SelectAll());
            editMenu.Opening += (s, e) =>
            {
                cut.Enabled = !ReadOnly && SelectionLength > 0;
                copy.Enabled = SelectionLength > 0;
                paste.Enabled = !ReadOnly && Enabled;
            };
            ContextMenuStrip = editMenu;
            ApplyLanguage();
        }

        internal void ApplyLanguage()
        {
            editMenu.Items[0].Text = I18n.T("剪切", "Cut");
            editMenu.Items[1].Text = I18n.T("复制", "Copy");
            editMenu.Items[2].Text = I18n.T("粘贴文件或路径", "Paste file or path");
            editMenu.Items[4].Text = I18n.T("全选", "Select all");
            CueText = I18n.T("Ctrl+V 粘贴文件或路径，也可拖入文件", "Ctrl+V to paste a file or path, or drop a file here");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) editMenu.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SendMessage(Handle, 0x1501, new IntPtr(1), CueText ?? string.Empty); // EM_SETCUEBANNER
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.V) || keyData == (Keys.Shift | Keys.Insert))
            {
                PasteFromClipboard();
                return true;
            }
            return base.ProcessCmdKey(ref message, keyData);
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmPaste)
            {
                PasteFromClipboard();
                return;
            }
            base.WndProc(ref message);
        }

        private void PasteFromClipboard()
        {
            if (ReadOnly || !Enabled) return;
            try { PasteData(Clipboard.GetDataObject()); }
            catch (ExternalException) { InputRejected?.Invoke(I18n.T("剪贴板正在被其他程序使用，请再粘贴一次。", "The clipboard is busy. Please try pasting again.")); }
        }

        internal void PasteData(IDataObject data)
        {
            if (data == null || ReadOnly || !Enabled) return;
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                SetDroppedFiles(data.GetData(DataFormats.FileDrop) as string[]);
                return;
            }
            string text = data.GetData(DataFormats.UnicodeText, true) as string;
            if (text == null) return;
            text = NormalizePathText(text);
            if (text.IndexOfAny(new[] { '\r', '\n' }) >= 0)
            {
                InputRejected?.Invoke(I18n.T("每个输入框只能填写一个文件路径。", "Enter one file path per field."));
                return;
            }
            SelectedText = text;
        }

        internal static string NormalizePathText(string text)
        {
            // 只移除复制路径时的包装，不修剪真实文件名中的空格。
            text = text.Trim('\r', '\n');
            if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
            {
                text = text.Substring(1, text.Length - 2);
            }
            return text;
        }

        private void SetDroppedFiles(string[] files)
        {
            if (files == null || files.Length != 1)
            {
                InputRejected?.Invoke(I18n.T("一次请粘贴或拖入一个 VPK 文件。", "Paste or drop one VPK file at a time."));
                return;
            }
            Text = files[0];
            SelectionStart = TextLength;
            ScrollToCaret();
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            e.Effect = !ReadOnly && Enabled && e.Data.GetDataPresent(DataFormats.FileDrop) &&
                (e.AllowedEffect & DragDropEffects.Copy) != 0 ? DragDropEffects.Copy : DragDropEffects.None;
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            if (!ReadOnly && Enabled) SetDroppedFiles(e.Data.GetData(DataFormats.FileDrop) as string[]);
        }
    }
}
