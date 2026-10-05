const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');

function createWindow () {
  const win = new BrowserWindow({
    width: 1200,
    height: 768,
    titleBarStyle: 'hidden',
    titleBarOverlay: {
      color: '#00000000', /* Transparent so the CSS glassmorphism shows through */
      symbolColor: '#ffffff',
      height: 48
    },
    icon: path.join(__dirname, 'al.png'),
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    }
  });

  win.loadFile('index.html');
}

app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});
