# L4N Icon Patcher · L4N 图标补全

[English](README.en.md) | 简体中文

轻量 Windows 工具，为《求生之路 2》的 **L4N Survivor 拓展模型**补齐头像与倒地图标。原生 WinForms 界面，无需安装，支持中英文即时切换。

## 下载与运行

到 [Releases](https://github.com/Kokkorylien/L4NIconPatcher/releases/latest) 下载 `L4NIconPatcher-v1.1.0-win.zip`，**完整解压**后运行 `L4NIconPatcher.exe`。

- 运行环境：Windows 10/11、.NET Framework 4.8。
- 发布包已包含 `bin/vpk.exe` 和所需 DLL，请保持目录结构完整。
- 右上角点击 **English / 简体中文** 切换语言；选择会在下次启动时保留。

## 使用

1. **源图标**：在资源管理器中复制包含生还者头像的 VPK，在第一个输入框按 **Ctrl+V**。
2. **目标模型**：在第二个输入框粘贴需要补全图标的 L4N 模型 VPK。
3. 点击 **生成 VPK**。完成后点击 **打开生成位置** 查看结果。

也支持粘贴带引号的文件路径、手动输入、拖入文件，或点击“浏览”。每个输入框接收一个文件。

源包应包含 `materials/vgui/s_panel_*.vtf`，目标包应包含 `models/l4n/s/<模型名>/`。工具从源包选择一组头像和倒地图标，为目标包中每个模型生成对应的 VTF/VMT；它不会自动判断源包中不同角色与目标模型的配对关系。没有单独的倒地图标时会复用头像。

输出保存在目标文件旁，名称为 `<目标名称> [+图标].vpk`，中英文模式使用同一个命名规则。源 VPK 和目标 VPK 保持不变；再次生成会替换同名输出 VPK。

## 路径保护

- 解包前检查输入名称和 VPK 索引，拒绝纯空白、尾随空格/句点、保留设备名及不安全路径。
- VPK 格式中用于表示根目录的单空格标记会被正常识别。
- 已有同名解包目录或输出目录时停止处理；请先移动或重命名，程序不会删除已有目录。
- 只清理本次生成的临时目录，清理失败会显示错误。

## 从源码构建

安装 Visual Studio 2022 的“.NET 桌面开发”工作负载及 .NET Framework 4.8 开发工具。

```powershell
msbuild L4NIconPatcher.sln /p:Configuration=Release
msbuild tests/BoundaryTests.csproj /p:Configuration=Release
./tests/bin/BoundaryTests.exe
./scripts/build-release.ps1
```

源码内的 `tools/vpk/` 会复制到输出的 `bin/`。打包脚本自动寻找 MSBuild，生成带运行依赖、双语 README 和第三方说明的便携 ZIP，以及 SHA-256 校验文件。测试自行生成最小 VPK，覆盖危险路径、文件粘贴、语言切换与偏好保存，以及调用真实 vpk.exe 的完整生成和内容校验。

## 第三方组件

`vpk.exe` 和随附 DLL 属于 Valve；版权及适用条款见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。本工具不附带游戏或创意工坊模型内容。

[作者主页](https://space.bilibili.com/2100612)
