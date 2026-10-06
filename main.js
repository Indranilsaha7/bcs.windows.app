const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const { autoUpdater } = require('electron-updater');

let mainWindow;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 1024,
    minHeight: 768,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    }
  });

  mainWindow.loadFile('index.html');
  
  // After the app loads, silently check for updates in the background
  mainWindow.webContents.once('did-finish-load', () => {
    autoUpdater.checkForUpdatesAndNotify();
  });
}

app.whenReady().then(() => {
  createWindow();

  app.on('activate', function () {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', function () {
  if (process.platform !== 'darwin') app.quit();
});

// --- AUTO UPDATER EVENTS ---

// When an update is found (but not yet downloaded)
autoUpdater.on('update-available', (info) => {
  if (mainWindow) mainWindow.webContents.send('update_available', info);
});

// When the update is fully downloaded and ready to install
autoUpdater.on('update-downloaded', (info) => {
  if (mainWindow) mainWindow.webContents.send('update_downloaded', info);
});

// Listen for the UI button click to restart and install
ipcMain.on('restart_app', () => {
  autoUpdater.quitAndInstall();
});
