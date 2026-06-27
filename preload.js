const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('api', {
  loadTodos: () => ipcRenderer.invoke('todos:load'),
  saveTodos: (todos) => ipcRenderer.send('todos:save', todos),
  setOpacity: (value) => ipcRenderer.send('win:setOpacity', value),
  getAutostart: () => ipcRenderer.invoke('autostart:get'),
  setAutostart: (enabled) => ipcRenderer.send('autostart:set', enabled),
  closeWin: () => ipcRenderer.send('win:close'),
  minimizeWin: () => ipcRenderer.send('win:minimize'),
});
