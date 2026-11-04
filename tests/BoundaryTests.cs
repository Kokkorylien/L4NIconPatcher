using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using L4NIconPatcher;

internal static class BoundaryTests
{
    private static int assertions;

    [STAThread]
    private static int Main(string[] args)
    {
        // 用本进程模拟正常退出、却没有创建解包目录的外部工具。
        if (args.Length == 1 && File.Exists(args[0])) return 0;
        WindowsFormsSynchronizationContext.AutoInstall = false;
        string projectRoot = Path.GetFullPath(args.Length == 0 ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..") : args[0]);
        string work = Path.Combine(projectRoot, "obj", "BoundaryTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            TestNames(work);
            TestArchivePaths(work);
            TestFileInput(projectRoot);
            TestLanguage(projectRoot);
            bool previousLanguage = I18n.English;
            try
            {
                I18n.English = true;
                TestRealVpk(projectRoot, work);
            }
            finally { I18n.English = previousLanguage; }
            Console.WriteLine("PASS: " + assertions + " assertions (including real vpk.exe extraction and packing).");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            Console.Error.WriteLine("Test workspace: " + work);
            return 1;
        }
        finally
        {
            Directory.Delete(work, true);
        }
    }

    private static void TestLanguage(string projectRoot)
    {
        I18n.LoadLanguage();
        bool originalLanguage = I18n.English;
        string previousPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", Path.Combine(projectRoot, "bin", "Release", "bin") + ";" + previousPath);
        try
        {
            I18n.English = false;
            using (var form = new Form1())
            {
                var source = (TextBox)typeof(Form1).GetField("textBoxSource", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                source.Text = @"E:\模型\source.vpk";
                I18n.English = true;
                Call(form, "ApplyLanguage");
                Check(form.Text == "L4N Icon Patcher", "English title applies immediately");
                Check(source.Text == @"E:\模型\source.vpk", "language switch preserves the selected path");
                Check(source.ContextMenuStrip.Items[2].Text == "Paste file or path", "English context menu");
                var generate = (Button)typeof(Form1).GetField("buttonGenerate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Check(generate.Text == "Generate VPK", "English action button");
                using (var help = new UsageForm()) Check(help.Text == "How to use", "English help window");
                try { VpkPathSafety.GetExtractionDirectory(@"E:\ .vpk"); }
                catch (InvalidDataException ex) { Check(ex.Message.StartsWith("Unsafe"), "English path error"); }
                I18n.English = false;
                Call(form, "ApplyLanguage");
                Check(form.Text == "L4N 图标补全" && generate.Text == "生成 VPK", "switch back to Chinese");
            }
            I18n.English = true;
            I18n.SaveLanguage();
            I18n.English = false;
            I18n.LoadLanguage();
            Check(I18n.English, "language preference survives reload");
        }
        finally
        {
            I18n.English = originalLanguage;
            I18n.SaveLanguage();
            Environment.SetEnvironmentVariable("PATH", previousPath);
        }
    }

    private static void TestFileInput(string projectRoot)
    {
        Type inputType = typeof(Form1).Assembly.GetType("L4NIconPatcher.FilePathTextBox", true);
        using (var input = (TextBox)Activator.CreateInstance(inputType, true))
        {
            MethodInfo paste = inputType.GetMethod("PasteData", BindingFlags.NonPublic | BindingFlags.Instance);
            string path = @"E:\测试 文件\ .vpk";
            var fileData = new DataObject(DataFormats.FileDrop, new[] { path });
            input.Text = "previous";
            paste.Invoke(input, new object[] { fileData });
            Check(input.Text == path, "copied file replaces input with exact path");
            paste.Invoke(input, new object[] { new DataObject(DataFormats.FileDrop, new[] { path, path }) });
            Check(input.Text == path, "multiple copied files leave input unchanged");
            input.SelectAll();
            paste.Invoke(input, new object[] { new DataObject(DataFormats.UnicodeText, "\"E:\\测试 文件\\model.vpk\"\r\n") });
            Check(input.Text == @"E:\测试 文件\model.vpk", "quoted Unicode path paste");
            input.SelectAll();
            paste.Invoke(input, new object[] { new DataObject(DataFormats.UnicodeText, "first.vpk\nsecond.vpk") });
            Check(input.Text == @"E:\测试 文件\model.vpk", "multiline paste leaves input unchanged");
            input.ReadOnly = true;
            paste.Invoke(input, new object[] { fileData });
            Check(input.Text == @"E:\测试 文件\model.vpk", "busy input rejects paste");
        }

        string previousPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", Path.Combine(projectRoot, "bin", "Release", "bin") + ";" + previousPath);
        try
        {
            using (var main = new Form1())
            using (var help = new UsageForm())
            {
                var source = (TextBox)typeof(Form1).GetField("textBoxSource", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(main);
                Check(!source.ReadOnly, "main input is editable");
                Check(main.Controls.Count > 0 && help.Controls.Count > 0, "both windows initialize");
            }
        }
        finally { Environment.SetEnvironmentVariable("PATH", previousPath); }
    }

    private static void TestNames(string work)
    {
        foreach (string name in new[] { ".vpk", " .vpk", "   .vpk", "\u3000.vpk", "name .vpk", "name..vpk",
            "CON.vpk", "nul.backup.vpk", "COM1.vpk", "LPT².vpk", "wrong.txt" })
        {
            Reject(() => VpkPathSafety.GetExtractionDirectory(Path.Combine(work, name)), name);
        }
        foreach (string name in new[] { "normal.vpk", "normal file.VPK", "中文 模型.vpk", " name.vpk", "COM10.vpk" })
        {
            Check(VpkPathSafety.GetExtractionDirectory(Path.Combine(work, name)) ==
                Path.Combine(work, Path.GetFileNameWithoutExtension(name)), "accept " + name);
        }
    }

    private static void TestArchivePaths(string work)
    {
        string archive = Path.Combine(work, "index.vpk");
        foreach (uint version in new uint[] { 1, 2 })
        {
            WriteArchive(archive, " ", "addoninfo", "txt", version);
            VpkPathSafety.ValidateArchive(archive);
            Check(true, "root space marker, v" + version);
            WriteArchive(archive, "materials/vgui", "icon", "vtf", version, new byte[] { 1, 2, 3 });
            VpkPathSafety.ValidateArchive(archive);
            Check(true, "preload data, v" + version);
        }
        WriteArchive(archive, " ", "LICENSE", " ");
        VpkPathSafety.ValidateArchive(archive);
        Check(true, "extensionless root file");

        foreach (string path in new[] { "models/ /nested", "models/\u3000", "models/name ", "models/name.",
            "../outside", "/absolute", "C:/absolute", "models//nested", "models/CON", "models/NUL.txt", "models\\..\\outside" })
        {
            WriteArchive(archive, path, "file", "txt");
            Reject(() => VpkPathSafety.ValidateArchive(archive), "archive path " + path);
        }
        foreach (string name in new[] { " ", "..", "../outside", "name ", "name.", "AUX", "file:stream" })
        {
            WriteArchive(archive, "models", name, " ");
            Reject(() => VpkPathSafety.ValidateArchive(archive), "archive name " + name);
        }
        WriteArchive(archive, "models", "file.", "aux");
        VpkPathSafety.ValidateArchive(archive);
        Check(true, "validate the complete filename, not its stem or extension");
        WriteArchive(archive, "models", "file", "../vtf");
        Reject(() => VpkPathSafety.ValidateArchive(archive), "invalid extension");
        WriteArchive(archive, "models", "file", "txt");
        byte[] complete = File.ReadAllBytes(archive);
        foreach (int length in new[] { 0, 11, complete.Length - 1 })
        {
            File.WriteAllBytes(archive, complete.Take(length).ToArray());
            Reject(() => VpkPathSafety.ValidateArchive(archive), "truncated archive");
        }
    }

    private static void TestRealVpk(string projectRoot, string work)
    {
        string source = Path.Combine(work, "源 图标.vpk");
        string target = Path.Combine(work, "目标 模型.vpk");
        var form = (Form1)FormatterServices.GetUninitializedObject(typeof(Form1));
        typeof(Form1).GetField("vpkExePath", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(form,
            Path.Combine(projectRoot, "bin", "Release", "bin", "vpk.exe"));

        // 从自有最小数据生成真实 VPK，不依赖本机的创意工坊模型文件。
        string sourceFixture = Path.Combine(work, "fixture-source");
        string targetFixture = Path.Combine(work, "fixture-target");
        string iconFixture = Path.Combine(sourceFixture, "materials", "vgui");
        Directory.CreateDirectory(iconFixture);
        File.WriteAllBytes(Path.Combine(iconFixture, "s_panel_gambler.vtf"), new byte[] { 1, 2, 3, 4 });
        File.WriteAllBytes(Path.Combine(iconFixture, "s_panel_gambler_incap.vtf"), new byte[] { 5, 6, 7, 8 });
        string modelFixture = Path.Combine(targetFixture, "models", "l4n", "s", "ko_ananta_tafei");
        Directory.CreateDirectory(modelFixture);
        File.WriteAllText(Path.Combine(modelFixture, "model.mdl"), "test model payload");
        File.WriteAllText(Path.Combine(targetFixture, "addoninfo.txt"), "test addon metadata");
        Call(form, "CreateVpkSync", sourceFixture, source);
        Call(form, "CreateVpkSync", targetFixture, target);

        string sentinel = Path.Combine(work, "keep.txt");
        File.WriteAllText(sentinel, "keep");
        foreach (string name in new[] { ".vpk", " .vpk", "name .vpk", "name..vpk" })
        {
            string bad = Path.Combine(work, name);
            File.Copy(source, bad);
            Reject(() => Call(form, "ExtractVpkSync", bad), "reject before extracting " + name);
            Reject(() => ((Task)Call(form, "GenerateVpkAsync", source, bad)).GetAwaiter().GetResult(),
                "reject target before extracting source " + name);
            Reject(() => ((Task)Call(form, "GenerateVpkAsync", bad, target)).GetAwaiter().GetResult(),
                "reject source before extracting " + name);
            Check(!Directory.Exists(VpkPathSafety.GetExtractionDirectory(source)), "no source side effects");
            Check(File.ReadAllText(sentinel) == "keep" && File.Exists(source), "parent contents survive");
        }
        Check(!Directory.GetDirectories(work).Any(d => string.IsNullOrWhiteSpace(Path.GetFileName(d))), "no blank directory");

        string sourceDirectory = VpkPathSafety.GetExtractionDirectory(source);
        Directory.CreateDirectory(sourceDirectory);
        string existing = Path.Combine(sourceDirectory, "existing.txt");
        File.WriteAllText(existing, "existing");
        Reject(() => ((Task<Dictionary<string, byte[]>>)Call(form, "ExtractIconsFromSourceAsync", source)).GetAwaiter().GetResult(),
            "existing extraction directory");
        Check(File.ReadAllText(existing) == "existing", "existing directory survives failure cleanup");
        Directory.Delete(sourceDirectory, true);

        string similarDirectory = sourceDirectory + "-unrelated";
        Directory.CreateDirectory(similarDirectory);
        File.WriteAllText(Path.Combine(similarDirectory, "keep.txt"), "keep");
        var toolField = typeof(Form1).GetField("vpkExePath", BindingFlags.NonPublic | BindingFlags.Instance);
        string realTool = (string)toolField.GetValue(form);
        toolField.SetValue(form, Assembly.GetExecutingAssembly().Location);
        try
        {
            Reject(() => ((Task<Dictionary<string, byte[]>>)Call(form, "ExtractIconsFromSourceAsync", source)).GetAwaiter().GetResult(),
                "missing exact output does not adopt similarly named directory");
            Check(File.ReadAllText(Path.Combine(similarDirectory, "keep.txt")) == "keep", "prefix directory survives failure");
        }
        finally { toolField.SetValue(form, realTool); }
        string unsafeArchive = Path.Combine(work, "unsafe.vpk");
        WriteArchive(unsafeArchive, "models/ /nested", "model", "mdl");
        Reject(() => Call(form, "ExtractVpkSync", unsafeArchive), "unsafe entry blocked before vpk.exe");
        Check(!Directory.Exists(VpkPathSafety.GetExtractionDirectory(unsafeArchive)), "unsafe archive creates no directory");

        var icons = ((Task<Dictionary<string, byte[]>>)Call(form, "ExtractIconsFromSourceAsync", source)).GetAwaiter().GetResult();
        Check(icons.ContainsKey("s_panel_gambler.vtf") && icons.ContainsKey("s_panel_gambler_incap.vtf"), "read real icons");
        Check(!Directory.Exists(sourceDirectory), "source extraction cleaned");
        Check(File.ReadAllText(Path.Combine(similarDirectory, "keep.txt")) == "keep", "similar directory untouched");
        var models = ((Task<List<string>>)Call(form, "GetModelFolderNamesAsync", target)).GetAwaiter().GetResult();
        Check(models.SequenceEqual(new[] { "ko_ananta_tafei" }), "read real model folder");
        Check(!Directory.Exists(VpkPathSafety.GetExtractionDirectory(target)), "target extraction cleaned");

        string existingOutput = Path.Combine(work, "目标 模型 [+图标]");
        Directory.CreateDirectory(existingOutput);
        File.WriteAllText(Path.Combine(existingOutput, "keep.txt"), "keep");
        using (var status = new Label())
        {
            typeof(Form1).GetField("labelStatus", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(form, status);
            Reject(() => ((Task)Call(form, "GenerateVpkAsync", source, target)).GetAwaiter().GetResult(), "existing output directory");
        }
        Check(File.ReadAllText(Path.Combine(existingOutput, "keep.txt")) == "keep", "existing output preserved");
        Directory.Delete(existingOutput, true);

        Type buttonType = typeof(Form1).Assembly.GetType("L4NIconPatcher.ModernButton", true);
        using (var status = new Label())
        using (var resultButton = (Button)Activator.CreateInstance(buttonType, true))
        {
            typeof(Form1).GetField("labelStatus", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(form, status);
            typeof(Form1).GetField("buttonOpenLastFolder", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(form, resultButton);
            ((Task)Call(form, "GenerateVpkAsync", source, target)).GetAwaiter().GetResult();
            string resultVpk = existingOutput + ".vpk";
            Check(File.Exists(resultVpk) && resultButton.Enabled, "full generation completes without a success dialog");
            Check(!Directory.Exists(existingOutput), "generation cleans its staging directory");
            string resultDirectory = (string)Call(form, "ExtractVpkSync", resultVpk);
            string resultIcons = Path.Combine(resultDirectory, "materials", "vgui", "l4n_s");
            Check(File.ReadAllBytes(Path.Combine(resultIcons, models[0] + ".vtf")).SequenceEqual(icons["s_panel_gambler.vtf"]),
                "generated avatar contains the source icon bytes");
            Check(File.ReadAllBytes(Path.Combine(resultIcons, models[0] + "_incap.vtf")).SequenceEqual(icons["s_panel_gambler_incap.vtf"]),
                "generated incap contains the source icon bytes");
            Check(File.ReadAllText(Path.Combine(resultIcons, models[0] + ".vmt")).Contains("vgui/l4n_s/" + models[0]),
                "generated material references the model icon");
            Directory.Delete(resultDirectory, true);
        }

        string extracted = (string)Call(form, "ExtractVpkSync", target);
        Check(File.Exists(Path.Combine(extracted, "addoninfo.txt")), "root marker extracts to root");
        string packed = Path.Combine(work, "roundtrip.vpk");
        Call(form, "CreateVpkSync", extracted, packed);
        VpkPathSafety.ValidateArchive(packed);
        string roundtrip = (string)Call(form, "ExtractVpkSync", packed);
        string[] originalFiles = Directory.GetFiles(extracted, "*", SearchOption.AllDirectories);
        Check(originalFiles.Length == Directory.GetFiles(roundtrip, "*", SearchOption.AllDirectories).Length, "roundtrip file count");
        foreach (string file in originalFiles)
        {
            Check(File.ReadAllBytes(file).SequenceEqual(File.ReadAllBytes(Path.Combine(roundtrip, file.Substring(extracted.Length + 1)))),
                "roundtrip bytes " + Path.GetFileName(file));
        }
    }

    private static object Call(Form1 form, string method, params object[] args)
    {
        try
        {
            return typeof(Form1).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, args);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException;
        }
    }

    private static void Reject(Action action, string description)
    {
        try { action(); }
        catch (InvalidDataException) { assertions++; return; }
        catch (IOException) { assertions++; return; }
        throw new Exception("Expected rejection: " + description);
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception("Failed: " + description);
        assertions++;
    }

    private static void WriteArchive(string path, string directory, string name, string extension,
        uint version = 1, byte[] preload = null)
    {
        preload = preload ?? new byte[0];
        using (var tree = new MemoryStream())
        using (var writer = new BinaryWriter(tree, Encoding.UTF8, true))
        {
            foreach (string token in new[] { extension, directory, name })
            {
                writer.Write(Encoding.UTF8.GetBytes(token));
                writer.Write((byte)0);
            }
            writer.Write((uint)0);
            writer.Write((ushort)preload.Length);
            writer.Write((ushort)0x7fff);
            writer.Write((uint)0);
            writer.Write((uint)0);
            writer.Write((ushort)0xffff);
            writer.Write(preload);
            writer.Write(new byte[3]);
            using (var archive = new BinaryWriter(File.Create(path)))
            {
                archive.Write((uint)0x55aa1234);
                archive.Write(version);
                archive.Write((uint)tree.Length);
                if (version == 2) archive.Write(new byte[16]);
                archive.Write(tree.ToArray());
            }
        }
    }
}
