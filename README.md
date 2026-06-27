# CC-Desktop-TODO

极简的 Windows 桌面 TODO 应用 —— 像便利贴一样置顶悬浮在桌面上。基于 Electron。

## 功能

- ✅ 增删改 + 勾选完成（双击任务文字可编辑）
- 💾 本地持久化（关闭重开数据还在，自动记住窗口位置/尺寸）
- 🪟 无边框、半透明、始终置顶，可拖动到桌面任意位置
- 🎚️ 透明度可调（顶栏滑块）
- 🚀 开机自启动（顶栏 ⏻ 按钮，打包安装后生效最稳定）

## 开发运行

```bash
npm install
npm start
```

> 国内网络如果 Electron 二进制下载失败，仓库内 `.npmrc` 已配置 npmmirror 镜像；
> 若仍失败，可手动设置环境变量：
> `set ELECTRON_MIRROR=https://npmmirror.com/mirrors/electron/`

## 打包（生成 Windows 安装包）

```bash
npm run dist
```

产物在 `dist/` 目录：

- `桌面TODO Setup 1.0.0.exe` —— NSIS 安装包，可选安装目录、自动建快捷方式
- `win-unpacked/桌面TODO.exe` —— 免安装版，直接运行

> electron-builder 的二进制（winCodeSign / NSIS）默认走 GitHub，国内可设置：
> `set ELECTRON_BUILDER_BINARIES_MIRROR=https://npmmirror.com/mirrors/electron-builder-binaries/`

## 界面说明

顶栏从左到右：标题、透明度滑块、开机自启开关（⏻）、最小化（—）、关闭（✕）。
拖动顶栏移动窗口；输入框回车添加任务；hover 任务项出现删除按钮；双击任务文字进入编辑（回车保存 / Esc 取消）。

## 技术栈

- Electron 33
- 渲染进程安全配置：`contextIsolation: true`、`nodeIntegration: false`，
  所有文件/系统操作经 `preload.js` 的 `contextBridge` 暴露受限 API
- 数据存储：`app.getPath('userData')/todos.json`，零额外依赖（Node 原生 `fs`）

## License

MIT
