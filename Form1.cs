using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace L4NIconPatcher
{
    public partial class Form1 : Form
    {
        private string vpkExePath = string.Empty;
        private string lastGeneratedVpkPath = string.Empty;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetShortPathNameW(string lpszLongPath, StringBuilder lpszShortPath, uint cchBuffer);

        public Form1()
        {
            InitializeComponent();
            ApplyLanguage();
            FindVpkExe();
            UpdateLastGeneratedButtonState();
        }

        private void FindVpkExe()
        {
            string exePath = Path.Combine(Application.StartupPath, "bin", "vpk.exe");
            if (File.Exists(exePath))
            {
                vpkExePath = exePath;
                return;
            }

            string envPath = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                foreach (string path in envPath.Split(';'))
                {
                    string testPath = Path.Combine(path.Trim(), "vpk.exe");
                    if (File.Exists(testPath))
                    {
                        vpkExePath = testPath;
                        return;
                    }
                }
            }

            DialogResult result = MessageBox.Show(
                I18n.T("未找到 vpk.exe，是否手动选择其位置？\n（通常位于 Steam 的 Left 4 Dead 2/bin/ 目录下）", "vpk.exe was not found. Would you like to locate it?\nIt is usually in Steam's Left 4 Dead 2/bin/ folder."),
                I18n.T("找不到 vpk.exe", "vpk.exe not found"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "vpk.exe|vpk.exe";
                    ofd.Title = I18n.T("请选择 vpk.exe", "Select vpk.exe");
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        vpkExePath = ofd.FileName;
                    }
                }
            }
        }

        // ============ VPK 操作方法 ============

        private string ExtractVpkSync(string vpkPath)
        {
            string extractDir = VpkPathSafety.ValidateArchive(vpkPath);
            if (Directory.Exists(extractDir) || File.Exists(extractDir))
            {
                throw new IOException(I18n.F("解包目录已存在，请先移动或重命名该目录，程序不会删除已有内容：\n{0}", "The extraction path already exists. Move or rename it first; existing contents will be preserved:\n{0}", extractDir));
            }

            try
            {
                RunVpkCommandSync($"\"{Path.GetFullPath(vpkPath)}\"");
                if (!Directory.Exists(extractDir))
                {
                    throw new IOException(I18n.F("解包失败：未创建预期目录 {0}", "Extraction failed: the expected folder was not created: {0}", extractDir));
                }
                return extractDir;
            }
            catch
            {
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                }
                throw;
            }
        }

        private void CreateVpkSync(string folderPath, string outputVpkPath)
        {
            string directoryName = Path.GetFileName(folderPath);
            string parentDirectory = Path.GetDirectoryName(folderPath);

            string currentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(parentDirectory);

                string args = $"\"{directoryName}\"";

                Process process = new Process();
                process.StartInfo.FileName = vpkExePath;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = parentDirectory;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                string generatedVpk = Path.Combine(parentDirectory, $"{directoryName}.vpk");

                if (!File.Exists(generatedVpk))
                {
                    string shortFolderName = GetShortPathName(folderPath);
                    if (!string.IsNullOrEmpty(shortFolderName))
                    {
                        string shortDirName = Path.GetFileName(shortFolderName);
                        args = $"\"{shortDirName}\"";

                        Process process2 = new Process();
                        process2.StartInfo.FileName = vpkExePath;
                        process2.StartInfo.Arguments = args;
                        process2.StartInfo.UseShellExecute = false;
                        process2.StartInfo.RedirectStandardOutput = true;
                        process2.StartInfo.RedirectStandardError = true;
                        process2.StartInfo.CreateNoWindow = true;
                        process2.StartInfo.WorkingDirectory = parentDirectory;

                        process2.Start();
                        output = process2.StandardOutput.ReadToEnd();
                        error = process2.StandardError.ReadToEnd();
                        process2.WaitForExit();

                        generatedVpk = Path.Combine(parentDirectory, $"{shortDirName}.vpk");
                    }
                }

                if (File.Exists(generatedVpk))
                {
                    if (!string.Equals(generatedVpk, outputVpkPath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(outputVpkPath))
                        {
                            File.Delete(outputVpkPath);
                        }
                        File.Move(generatedVpk, outputVpkPath);
                    }
                }
                else
                {
                    string[] vpkFiles = Directory.GetFiles(parentDirectory, "*.vpk", SearchOption.TopDirectoryOnly);
                    foreach (string vpkFile in vpkFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(vpkFile);
                        if (fileName.StartsWith(directoryName) || directoryName.StartsWith(fileName))
                        {
                            if (!string.Equals(vpkFile, outputVpkPath, StringComparison.OrdinalIgnoreCase))
                            {
                                if (File.Exists(outputVpkPath))
                                {
                                    File.Delete(outputVpkPath);
                                }
                                File.Move(vpkFile, outputVpkPath);
                                return;
                            }
                        }
                    }

                    throw new Exception(I18n.T("打包失败：未能生成 VPK 文件。", "Packing failed: no VPK file was created."));
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }
        }

        private string GetShortPathName(string longPath)
        {
            try
            {
                StringBuilder sb = new StringBuilder(260);
                uint result = GetShortPathNameW(longPath, sb, (uint)sb.Capacity);
                if (result > 0 && result < sb.Capacity)
                {
                    return sb.ToString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void RunVpkCommandSync(string arguments)
        {
            Process process = new Process();
            process.StartInfo.FileName = vpkExePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(vpkExePath);

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error) && !error.Contains("couldn't find file"))
            {
                throw new Exception(I18n.F("vpk.exe 执行失败\n命令：{0}\n错误：{1}\n输出：{2}", "vpk.exe failed\nArguments: {0}\nError: {1}\nOutput: {2}", arguments, error, output));
            }
        }

        // ============ 核心逻辑方法 ============

        private async Task<Dictionary<string, byte[]>> ExtractIconsFromSourceAsync(string vpkPath)
        {
            return await Task.Run(() =>
            {
                var icons = new Dictionary<string, byte[]>();
                string extractDir = null;

                try
                {
                    extractDir = ExtractVpkSync(vpkPath);

                    string vguiPath = Path.Combine(extractDir, "materials", "vgui");
                    if (!Directory.Exists(vguiPath))
                    {
                        string[] possiblePaths = {
                            Path.Combine(extractDir, "materials", "vgui"),
                            Path.Combine(extractDir, "materials", "VGUI"),
                        };

                        foreach (string path in possiblePaths)
                        {
                            if (Directory.Exists(path))
                            {
                                vguiPath = path;
                                break;
                            }
                        }

                        if (!Directory.Exists(vguiPath))
                        {
                            string[] vtfFiles = Directory.GetFiles(extractDir, "s_panel*.vtf", SearchOption.AllDirectories);
                            if (vtfFiles.Length > 0)
                            {
                                foreach (string vtf in vtfFiles)
                                {
                                    string fileName = Path.GetFileName(vtf);
                                    icons[fileName] = File.ReadAllBytes(vtf);
                                }
                                return icons;
                            }
                            throw new Exception(I18n.F("未找到 materials/vgui 目录。解包目录：{0}", "materials/vgui was not found in the extracted folder: {0}", extractDir));
                        }
                    }

                    string[] iconNames = {
                        "s_panel_namvet", "s_panel_teenangst", "s_panel_biker", "s_panel_manager",
                        "s_panel_coach", "s_panel_gambler", "s_panel_mechanic", "s_panel_producer"
                    };

                    foreach (string name in iconNames)
                    {
                        string avatarPath = Path.Combine(vguiPath, $"{name}.vtf");
                        if (File.Exists(avatarPath))
                        {
                            icons[$"{name}.vtf"] = File.ReadAllBytes(avatarPath);
                        }

                        string incapPath = Path.Combine(vguiPath, $"{name}_incap.vtf");
                        if (File.Exists(incapPath))
                        {
                            icons[$"{name}_incap.vtf"] = File.ReadAllBytes(incapPath);
                        }
                    }

                    if (icons.Count == 0)
                    {
                        string[] allVtf = Directory.GetFiles(vguiPath, "*.vtf");
                        if (allVtf.Length > 0)
                        {
                            foreach (string vtf in allVtf)
                            {
                                string fileName = Path.GetFileName(vtf);
                                icons[fileName] = File.ReadAllBytes(vtf);
                            }
                        }
                        else
                        {
                            throw new Exception(I18n.F("在 {0} 中未找到任何 VTF 文件。", "No VTF files were found in {0}.", vguiPath));
                        }
                    }

                    return icons;
                }
                finally
                {
                    if (extractDir != null) Directory.Delete(extractDir, true);
                }
            });
        }

        private async Task<List<string>> GetModelFolderNamesAsync(string vpkPath)
        {
            return await Task.Run(() =>
            {
                var folders = new HashSet<string>();
                string extractDir = null;

                try
                {
                    extractDir = ExtractVpkSync(vpkPath);

                    string basePath = Path.Combine(extractDir, "models", "l4n", "s");
                    if (Directory.Exists(basePath))
                    {
                        foreach (string dir in Directory.GetDirectories(basePath))
                        {
                            string folderName = Path.GetFileName(dir);
                            if (!string.IsNullOrWhiteSpace(folderName))
                            {
                                folders.Add(folderName);
                            }
                        }
                    }
                    else
                    {
                        string[] searchPaths = {
                            Path.Combine(extractDir, "models", "l4n", "s"),
                            Path.Combine(extractDir, "models", "L4N", "S"),
                            Path.Combine(extractDir, "models", "l4n"),
                            Path.Combine(extractDir, "models"),
                        };

                        foreach (string searchPath in searchPaths)
                        {
                            if (Directory.Exists(searchPath))
                            {
                                foreach (string dir in Directory.GetDirectories(searchPath))
                                {
                                    string folderName = Path.GetFileName(dir);
                                    if (!string.IsNullOrWhiteSpace(folderName))
                                    {
                                        folders.Add(folderName);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    if (extractDir != null) Directory.Delete(extractDir, true);
                }

                return folders.ToList();
            });
        }

        /// <summary>
        /// 生成新 VPK 的核心逻辑（优化版）
        /// </summary>
        private async Task GenerateVpkAsync(string sourcePath, string targetPath)
        {
            if (string.IsNullOrEmpty(vpkExePath) || !File.Exists(vpkExePath))
            {
                throw new Exception(I18n.T("未找到 vpk.exe，请确保 bin 文件夹中存在 vpk.exe", "vpk.exe was not found. Keep it in the application's bin folder."));
            }

            // 两个输入的名称都在产生任何文件系统副作用之前检查。
            VpkPathSafety.GetExtractionDirectory(sourcePath);
            VpkPathSafety.GetExtractionDirectory(targetPath);

            // 1. 提取源图标
            UpdateStatus(I18n.T("正在提取源图标…", "Extracting source icons…"));
            var icons = await ExtractIconsFromSourceAsync(sourcePath);

            if (icons.Count == 0)
            {
                throw new Exception(I18n.T("未找到任何生还者图标，请确认源 VPK 是否正确。", "No survivor icons were found. Check the source VPK."));
            }

            UpdateStatus(I18n.F("找到 {0} 个图标文件", "Found {0} icon files", icons.Count));

            // 2. 获取目标模型文件夹名称
            UpdateStatus(I18n.T("正在读取目标 VPK 结构…", "Reading the target VPK structure…"));
            var modelFolders = await GetModelFolderNamesAsync(targetPath);

            if (modelFolders.Count == 0)
            {
                throw new Exception(I18n.T("目标 VPK 中未找到 models/l4n/s/ 下的模型文件夹。", "No model folders were found under models/l4n/s/ in the target VPK."));
            }

            UpdateStatus(I18n.F("找到 {0} 个模型文件夹", "Found {0} model folders", modelFolders.Count));

            // 3. 准备路径
            string targetDir = Path.GetDirectoryName(targetPath);
            string baseName = Path.GetFileNameWithoutExtension(targetPath);

            // 原始解包目录（由 vpk.exe 自动创建）
            string extractDir = null;

            // 新文件夹名称（带 [+图标] 后缀）
            string newFolderName = $"{baseName} [+图标]";
            string newFolderPath = Path.Combine(targetDir, newFolderName);
            string newVpkPath = $"{newFolderPath}.vpk";

            // 已有目录不属于本次操作，不能递归删除。
            if (Directory.Exists(newFolderPath))
            {
                throw new IOException(I18n.F("输出目录已存在，请先移动或重命名该目录：\n{0}", "The output folder already exists. Move or rename it first:\n{0}", newFolderPath));
            }
            if (File.Exists(newVpkPath))
            {
                File.Delete(newVpkPath);
            }

            bool ownsNewFolder = false;
            try
            {
                // 4. 解包目标 VPK，只使用经过校验的精确目录。
                UpdateStatus(I18n.T("正在解包目标 VPK…", "Extracting the target VPK…"));
                extractDir = ExtractVpkSync(targetPath);

                // 5. ★★★ 核心优化：直接重命名文件夹 ★★★
                UpdateStatus(I18n.T("正在准备输出文件夹…", "Preparing the output folder…"));
                Directory.Move(extractDir, newFolderPath);
                ownsNewFolder = true;
                extractDir = null;

                // 6. 创建图标目录并生成图标文件
                UpdateStatus(I18n.F("正在创建图标（{0} 个模型文件夹）…", "Creating icons for {0} model folders…", modelFolders.Count));

                // 选择图标
                string avatarKey = icons.Keys.FirstOrDefault(k => k.EndsWith(".vtf") && !k.Contains("_incap"));
                string incapKey = icons.Keys.FirstOrDefault(k => k.EndsWith("_incap.vtf"));

                if (string.IsNullOrEmpty(avatarKey))
                {
                    avatarKey = icons.Keys.FirstOrDefault();
                    if (string.IsNullOrEmpty(avatarKey))
                    {
                        throw new Exception(I18n.T("未找到可用的图标文件", "No usable icon files were found."));
                    }
                }

                if (string.IsNullOrEmpty(incapKey))
                {
                    incapKey = icons.Keys.FirstOrDefault(k => k.Contains("incap"));
                    if (string.IsNullOrEmpty(incapKey))
                    {
                        incapKey = avatarKey;
                    }
                }

                int processed = 0;
                foreach (string folder in modelFolders)
                {
                    processed++;
                    UpdateStatus(I18n.F("正在处理 {0} ({1}/{2})…", "Processing {0} ({1}/{2})…", folder, processed, modelFolders.Count));

                    // 创建图标目录
                    string iconDir = Path.Combine(newFolderPath, "materials", "vgui", "l4n_s");
                    Directory.CreateDirectory(iconDir);

                    // 写入 VTF 文件
                    string avatarVtfPath = Path.Combine(iconDir, $"{folder}.vtf");
                    File.WriteAllBytes(avatarVtfPath, icons[avatarKey]);

                    string incapVtfPath = Path.Combine(iconDir, $"{folder}_incap.vtf");
                    File.WriteAllBytes(incapVtfPath, icons[incapKey]);

                    // 生成 VMT 材质文件
                    string avatarVmtPath = Path.Combine(iconDir, $"{folder}.vmt");
                    string incapVmtPath = Path.Combine(iconDir, $"{folder}_incap.vmt");

                    string vmtTemplate = @"""UnlitGeneric""
{{
    ""$ignorez""		""1""
    ""$basetexture""		""vgui/l4n_s/{0}""
    ""$translucent""		""1""
    ""$no_fullbright""		""1""
    ""$vertexcolor""		""1""
    ""$vertexalpha""		""1""
    ""$additive""		""0""
}}";

                    string vmtTemplateIncap = @"""UnlitGeneric""
{{
    ""$ignorez""		""1""
    ""$basetexture""		""vgui/l4n_s/{0}""
    ""$translucent""		""1""
    ""$no_fullbright""		""1""
    ""$vertexcolor""		""1""
}}";

                    File.WriteAllText(avatarVmtPath, string.Format(vmtTemplate, folder));
                    File.WriteAllText(incapVmtPath, string.Format(vmtTemplateIncap, folder + "_incap"));
                }

                // 7. 打包新 VPK
                UpdateStatus(I18n.T("正在打包新 VPK…", "Packing the new VPK…"));
                CreateVpkSync(newFolderPath, newVpkPath);

                if (!File.Exists(newVpkPath))
                {
                    throw new IOException(I18n.F("打包失败：未生成预期文件 {0}", "Packing failed: the expected file was not created: {0}", newVpkPath));
                }
            }
            finally
            {
                // 只清理本次操作取得的目录，清理失败必须向用户报告。
                if (extractDir != null) Directory.Delete(extractDir, true);
                if (ownsNewFolder) Directory.Delete(newFolderPath, true);
            }

            lastGeneratedVpkPath = newVpkPath;
            UpdateLastGeneratedButtonState();
            UpdateStatus(I18n.F("生成完成：{0}", "Created: {0}", newVpkPath));

        }

        private void OpenFolderAndSelectFile(string filePath)
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            catch (Exception ex)
            {
                try
                {
                    string folder = Path.GetDirectoryName(filePath);
                    if (Directory.Exists(folder))
                    {
                        Process.Start("explorer.exe", folder);
                    }
                }
                catch
                {
                    MessageBox.Show(I18n.F("无法打开文件夹：{0}", "Unable to open the folder: {0}", ex.Message), I18n.T("错误", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateLastGeneratedButtonState()
        {
            bool hasValidFile = !string.IsNullOrEmpty(lastGeneratedVpkPath) && File.Exists(lastGeneratedVpkPath);
            buttonOpenLastFolder.Enabled = hasValidFile;
        }

        private void UpdateStatus(string message)
        {
            if (labelStatus.InvokeRequired)
            {
                labelStatus.Invoke(new Action(() => { labelStatus.Text = message; }));
            }
            else
            {
                labelStatus.Text = message;
            }
        }

        // ============ 界面事件 ============

        private void ApplyLanguage()
        {
            SuspendLayout();
            I18n.Apply(this);
            buttonLanguage.Text = I18n.English ? "简体中文" : "English";
            buttonLanguage.AccessibleName = I18n.T("切换到英文", "Switch to Simplified Chinese");
            textBoxSource.AccessibleName = I18n.T("源图标 VPK 路径", "Source icons VPK path");
            textBoxTarget.AccessibleName = I18n.T("目标模型 VPK 路径", "Target model VPK path");
            textBoxSource.ApplyLanguage();
            textBoxTarget.ApplyLanguage();
            labelStatus.AccessibleName = I18n.T("处理状态", "Status");
            labelState.Text = I18n.T("就绪", "Ready");
            UpdateStatus(I18n.T("粘贴两个 VPK 文件，即可开始。", "Paste two VPK files to get started."));
            ResumeLayout(false);
            Invalidate(true);
        }

        private void buttonLanguage_Click(object sender, EventArgs e)
        {
            I18n.English = !I18n.English;
            ApplyLanguage();
            try { I18n.SaveLanguage(); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Configuration.ConfigurationErrorsException)
            {
                UpdateStatus(I18n.T("语言已切换，但无法保存偏好设置。", "Language changed, but the preference could not be saved."));
            }
        }

        private void InputRejected(string message)
        {
            labelState.Text = I18n.T("检查输入", "Check input");
            UpdateStatus(message);
        }

        private void InputPathChanged(object sender, EventArgs e)
        {
            labelState.Text = I18n.T("就绪", "Ready");
            UpdateStatus(I18n.T("确认两个 VPK 文件后，点击“生成 VPK”。", "Check both VPK files, then click Generate VPK."));
        }

        private void buttonSelectSource_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = I18n.T("VPK 文件|*.vpk|所有文件|*.*", "VPK files|*.vpk|All files|*.*");
                ofd.Title = I18n.T("选择生还者 VPK（源）", "Select the source icons VPK");
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxSource.Text = ofd.FileName;
                    UpdateStatus(I18n.F("源图标：{0}", "Source icons: {0}", Path.GetFileName(ofd.FileName)));
                }
            }
        }

        private void buttonSelectTarget_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = I18n.T("VPK 文件|*.vpk|所有文件|*.*", "VPK files|*.vpk|All files|*.*");
                ofd.Title = I18n.T("选择拓展模型 VPK（目标）", "Select the target L4N model VPK");
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxTarget.Text = ofd.FileName;
                    UpdateStatus(I18n.F("目标模型：{0}", "Target model: {0}", Path.GetFileName(ofd.FileName)));
                }
            }
        }

        private async void buttonGenerate_Click(object sender, EventArgs e)
        {
            string sourceVpkPath = FilePathTextBox.NormalizePathText(textBoxSource.Text);
            string targetVpkPath = FilePathTextBox.NormalizePathText(textBoxTarget.Text);
            textBoxSource.Text = sourceVpkPath;
            textBoxTarget.Text = targetVpkPath;
            if (string.IsNullOrEmpty(sourceVpkPath) || !File.Exists(sourceVpkPath))
            {
                InputRejected(I18n.T("请粘贴或选择有效的源 VPK 文件。", "Paste or select a valid source VPK file."));
                textBoxSource.Focus();
                return;
            }

            if (string.IsNullOrEmpty(targetVpkPath) || !File.Exists(targetVpkPath))
            {
                InputRejected(I18n.T("请粘贴或选择有效的目标 VPK 文件。", "Paste or select a valid target VPK file."));
                textBoxTarget.Focus();
                return;
            }

            if (string.IsNullOrEmpty(vpkExePath) || !File.Exists(vpkExePath))
            {
                MessageBox.Show(I18n.T("未找到 vpk.exe，请将 vpk.exe 放在程序目录的 bin 文件夹中。", "vpk.exe was not found. Place it in the application's bin folder."), I18n.T("错误", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            buttonGenerate.Enabled = false;
            buttonSelectSource.Enabled = false;
            buttonSelectTarget.Enabled = false;
            buttonLanguage.Enabled = false;
            textBoxSource.ReadOnly = true;
            textBoxTarget.ReadOnly = true;
            buttonGenerate.Text = I18n.T("正在生成…", "Generating…");
            labelState.Text = I18n.T("处理中", "Working");
            progressBar1.Visible = true;
            progressBar1.Style = ProgressBarStyle.Marquee;

            try
            {
                await GenerateVpkAsync(sourceVpkPath, targetVpkPath);
                labelState.Text = I18n.T("已完成", "Done");
            }
            catch (Exception ex)
            {
                MessageBox.Show(I18n.F("生成失败：{0}", "Generation failed: {0}", ex.Message), I18n.T("错误", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                labelState.Text = I18n.T("未完成", "Failed");
                UpdateStatus(I18n.F("错误：{0}", "Error: {0}", ex.Message));
            }
            finally
            {
                buttonGenerate.Enabled = true;
                buttonSelectSource.Enabled = true;
                buttonSelectTarget.Enabled = true;
                buttonLanguage.Enabled = true;
                textBoxSource.ReadOnly = false;
                textBoxTarget.ReadOnly = false;
                buttonGenerate.Text = I18n.T("生成 VPK", "Generate VPK");
                progressBar1.Visible = false;
                progressBar1.Style = ProgressBarStyle.Blocks;
                progressBar1.Value = 0;
            }
        }

        private void buttonOpenLastFolder_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(lastGeneratedVpkPath) && File.Exists(lastGeneratedVpkPath))
            {
                OpenFolderAndSelectFile(lastGeneratedVpkPath);
            }
            else
            {
                MessageBox.Show(I18n.T("没有可打开的文件。请先成功生成一个 VPK 文件。", "No output file is available. Generate a VPK first."), I18n.T("提示", "Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateLastGeneratedButtonState();
            }
        }

        private void buttonUsage_Click(object sender, EventArgs e)
        {
            using (UsageForm usageForm = new UsageForm())
            {
                usageForm.ShowDialog(this);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateStatus(I18n.T("粘贴两个 VPK 文件，即可开始。", "Paste two VPK files to get started."));
        }
    }
}
