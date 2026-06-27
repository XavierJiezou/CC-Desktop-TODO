# CC-Desktop-TODO

极简的 Windows 桌面 TODO 应用 —— 像便利贴一样置顶悬浮在桌面上。
原生 C# / WinForms 实现，**程序本体仅 18 KB，安装包仅 82 KB**，无需任何运行时下载。

<img width="399" height="524" alt="image" src="https://github.com/user-attachments/assets/3c5dc66e-edf4-4a8c-83ab-247a440bbb9c" />

## 功能

- ✅ 增删改 + 勾选完成（双击任务文字可编辑，回车保存 / Esc 取消）
- 💾 本地持久化（关闭重开数据还在，自动记住窗口位置 / 尺寸 / 透明度）
- 🪟 无边框、圆角、始终置顶，拖动顶栏可移动到桌面任意位置
- 🎚️ 透明度可调（顶栏滑块，25%–100%）
- 🚀 开机自启动（顶栏 ⏻ 按钮，写入注册表 Run 项）
- 🖱️ 右下角可拉伸缩放窗口

## 体积对比

| | 程序本体 | 安装包 | 额外运行时 |
|---|---|---|---|
| 本项目（C# WinForms） | **18 KB** | **82 KB** | 无（.NET Framework 4.x 系统自带） |
| 同类 Electron 方案 | ~180 MB | ~78 MB | 自带整套 Chromium |

## 下载使用

到 [Releases](https://github.com/XavierJiezou/CC-Desktop-TODO/releases) 下载
`CC-Desktop-TODO-Setup-1.0.0.exe`，双击进入安装向导（可选安装目录、自动创建快捷方式）。

> ⚠️ 安装包未做代码签名，首次运行 Windows SmartScreen 可能提示"未知发布者"，
> 点 **更多信息 → 仍要运行** 即可。

## 从源码编译

无需安装 Visual Studio 或 .NET SDK —— 直接用 Windows 自带的 C# 编译器：

```bat
cd src-csharp
build.bat
```

产物为 `build\DesktopTodo.exe`（约 18 KB）。

`build.bat` 调用系统内置的
`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe` 编译，
引用 `System.Windows.Forms.dll` / `System.Drawing.dll`。

## 打包安装向导

用 [NSIS](https://nsis.sourceforge.io/) 编译 `installer\setup.nsi`：

```bat
makensis installer\setup.nsi
```

产物为 `dist\CC-Desktop-TODO-Setup-1.0.0.exe`（约 82 KB），
基于 MUI2 现代向导：欢迎页 → 选目录 → 安装 → 完成，自带卸载程序并注册到"添加/删除程序"。

## 界面说明

顶栏从左到右：标题、透明度滑块、开机自启开关（⏻，开启时变蓝）、最小化（—）、关闭（✕）。
- 拖动顶栏移动窗口
- 输入框回车或点 ＋ 添加任务
- 鼠标悬停任务行出现删除按钮（✕）
- 双击任务文字进入编辑

## 技术栈

- C# 5 + WinForms（.NET Framework 4.x，Win10/11 预装）
- 数据存储：`%AppData%\CC-Desktop-TODO\`
  - `todos.txt` —— 每行 `0|文本`（未完成）或 `1|文本`（已完成）
  - `config.txt` —— 窗口位置 / 尺寸 / 透明度
- 开机自启：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- 圆角窗口：`CreateRoundRectRgn`；边缘缩放：`WM_NCHITTEST`

## License

[MIT](LICENSE)
