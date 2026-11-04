using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace L4NIconPatcher
{
    internal static class VpkPathSafety
    {
        internal static string GetExtractionDirectory(string vpkPath)
        {
            if (string.IsNullOrWhiteSpace(vpkPath) ||
                !string.Equals(Path.GetExtension(vpkPath), ".vpk", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(I18n.T("请选择有效的 .vpk 文件。", "Select a valid .vpk file."));
            }

            // 必须在 GetFullPath 的 Win32 规范化之前检查原始名称。
            ValidateName(Path.GetFileNameWithoutExtension(vpkPath));
            string root = Path.GetPathRoot(vpkPath);
            foreach (string part in vpkPath.Substring(root.Length).Split(new[] { '\\', '/' }))
            {
                ValidateName(part);
            }

            string fullPath = Path.GetFullPath(vpkPath);
            return Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath));
        }

        internal static string ValidateArchive(string vpkPath)
        {
            string extractDirectory = GetExtractionDirectory(vpkPath);
            using (var reader = new BinaryReader(File.OpenRead(vpkPath)))
            {
                if (reader.BaseStream.Length < 12 || reader.ReadUInt32() != 0x55AA1234)
                {
                    throw new InvalidDataException(I18n.T("VPK 文件头无效。", "Invalid VPK file header."));
                }

                uint version = reader.ReadUInt32();
                uint treeSize = reader.ReadUInt32();
                int headerSize;
                if (version == 1)
                {
                    headerSize = 12;
                }
                else if (version == 2)
                {
                    headerSize = 28;
                }
                else
                {
                    throw new InvalidDataException(I18n.T("不支持的 VPK 版本。", "Unsupported VPK version."));
                }

                long treeEnd = (long)headerSize + treeSize;
                if (treeSize == 0 || treeEnd > reader.BaseStream.Length)
                {
                    throw new InvalidDataException(I18n.T("VPK 目录索引不完整。", "The VPK directory index is incomplete."));
                }

                reader.BaseStream.Position = headerSize;
                string extension;
                while ((extension = ReadTreeString(reader, treeEnd)).Length > 0)
                {
                    string directory;
                    while ((directory = ReadTreeString(reader, treeEnd)).Length > 0)
                    {
                        // VPK 的单空格路径表示根目录，不能当成真实目录名。
                        string relativeDirectory = directory == " " ? string.Empty : directory;
                        if (relativeDirectory.Length > 0)
                        {
                            foreach (string part in relativeDirectory.Split(new[] { '/', '\\' }))
                            {
                                ValidateName(part);
                            }
                        }

                        string name;
                        while ((name = ReadTreeString(reader, treeEnd)).Length > 0)
                        {
                            string fileName = extension == " " ? name : name + "." + extension;
                            ValidateName(fileName);
                            string destination = Path.Combine(extractDirectory, relativeDirectory, fileName);
                            if (destination.Length >= 260)
                            {
                                throw new PathTooLongException(I18n.T("VPK 解包路径超出 vpk.exe 支持的长度：", "The extraction path exceeds the length supported by vpk.exe: ") + destination);
                            }

                            if (treeEnd - reader.BaseStream.Position < 18)
                            {
                                throw new InvalidDataException(I18n.T("VPK 文件条目不完整。", "The VPK file entry is incomplete."));
                            }
                            reader.ReadUInt32(); // CRC
                            ushort preloadBytes = reader.ReadUInt16();
                            reader.ReadUInt16(); // ArchiveIndex
                            reader.ReadUInt32(); // EntryOffset
                            reader.ReadUInt32(); // EntryLength
                            if (reader.ReadUInt16() != 0xffff || preloadBytes > treeEnd - reader.BaseStream.Position)
                            {
                                throw new InvalidDataException(I18n.T("VPK 文件条目无效。", "Invalid VPK file entry."));
                            }
                            if (preloadBytes > 0) reader.BaseStream.Position += preloadBytes;
                        }
                    }
                }
            }
            return extractDirectory;
        }

        private static string ReadTreeString(BinaryReader reader, long treeEnd)
        {
            var bytes = new List<byte>();
            while (reader.BaseStream.Position < treeEnd)
            {
                byte value = reader.ReadByte();
                if (value == 0)
                {
                    return Encoding.UTF8.GetString(bytes.ToArray());
                }
                bytes.Add(value);
                if (bytes.Count > 4 * 260)
                {
                    throw new PathTooLongException(I18n.T("VPK 目录索引中的名称过长。", "A name in the VPK directory index is too long."));
                }
            }
            throw new InvalidDataException(I18n.T("VPK 目录索引缺少结束标记。", "The VPK directory index is missing a terminator."));
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 255 ||
                name.EndsWith(" ", StringComparison.Ordinal) || name.EndsWith(".", StringComparison.Ordinal) ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidDataException(I18n.F("不安全的文件或文件夹名称：[{0}]。名称不能是空白、以空格或句点结尾，或含有 Windows 不支持的字符。", "Unsafe file or folder name: [{0}]. Names must not be blank, end in a space or period, or contain characters unsupported by Windows.", name));
            }

            string deviceName = name.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
            if (deviceName == "CON" || deviceName == "PRN" || deviceName == "AUX" || deviceName == "NUL" ||
                deviceName == "CONIN$" || deviceName == "CONOUT$" ||
                (deviceName.Length == 4 &&
                 (deviceName.StartsWith("COM", StringComparison.Ordinal) || deviceName.StartsWith("LPT", StringComparison.Ordinal)) &&
                 "123456789¹²³".IndexOf(deviceName[3]) >= 0))
            {
                throw new InvalidDataException(I18n.F("文件或文件夹名称是 Windows 保留设备名：[{0}]。", "The file or folder name is a reserved Windows device name: [{0}].", name));
            }
        }
    }
}
