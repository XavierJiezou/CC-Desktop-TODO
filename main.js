const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const fs = require('fs');

// 数据文件存到 userData 目录，避免污染源码目录
const dataFile = path.join(app.getPath('userData'), 'todos.json');

const DEFAULT_STATE = {
  todos: [],
  bounds: { width: 320, height: 420 },
  opacity: 0.85,
};

let win = null;
let saveTimer = null;

function readState() {
  try {
    const raw = fs.readFileSync(dataFile, 'utf-8');
    const parsed = JSON.parse(raw);
    return {
      todos: Array.isArray(parsed.todos) ? parsed.todos : [],
      bounds: { ...DEFAULT_STATE.bounds, ...(parsed.bounds || {}) },
      opacity: typeof parsed.opacity === 'number' ? parsed.opacity : DEFAULT_STATE.opacity,
    };
  } catch {
    return { ...DEFAULT_STATE, bounds: { ...DEFAULT_STATE.bounds } };
  }
}

function writeState(state) {
  try {
    fs.writeFileSync(dataFile, JSON.stringify(state, null, 2), 'utf-8');
  } catch (err) {
    console.error('保存失败:', err);
  }
}

// 把当前窗口位置/尺寸合并进数据文件（节流）
function persistBounds() {
  if (!win || win.isDestroyed()) return;
  if (saveTimer) clearTimeout(saveTimer);
  saveTimer = setTimeout(() => {
    const state = readState();
    state.bounds = win.getBounds();
    writeState(state);
  }, 400);
}

function createWindow() {
  const state = readState();
  const b = state.bounds;

  win = new BrowserWindow({
    width: b.width,
    height: b.height,
    x: b.x,
    y: b.y,
    minWidth: 220,
    minHeight: 180,
    frame: false,
    transparent: true,
    alwaysOnTop: true,
    resizable: true,
    skipTaskbar: true,
    hasShadow: false,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  win.loadFile('index.html');
  win.on('move', persistBounds);
  win.on('resize', persistBounds);
}

// ---- IPC ----
ipcMain.handle('todos:load', () => {
  const state = readState();
  return { todos: state.todos, opacity: state.opacity };
});

ipcMain.on('todos:save', (_e, todos) => {
  const state = readState();
  state.todos = Array.isArray(todos) ? todos : [];
  writeState(state);
});

ipcMain.on('win:setOpacity', (_e, value) => {
  const state = readState();
  state.opacity = value;
  writeState(state);
});

ipcMain.on('win:close', () => {
  if (win && !win.isDestroyed()) win.close();
});

ipcMain.on('win:minimize', () => {
  if (win && !win.isDestroyed()) win.minimize();
});

ipcMain.handle('autostart:get', () => {
  return app.getLoginItemSettings().openAtLogin;
});

ipcMain.on('autostart:set', (_e, enabled) => {
  app.setLoginItemSettings({ openAtLogin: !!enabled });
});

// ---- 生命周期 ----
app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
  app.quit();
});
