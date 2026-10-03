// Global Topbar / Titlebar Logic
const { ipcRenderer } = require('electron');

document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('btn-minimize')?.addEventListener('click', () => {
        ipcRenderer.send('window-minimize');
    });

    document.getElementById('btn-maximize')?.addEventListener('click', () => {
        ipcRenderer.send('window-maximize');
    });

    document.getElementById('btn-close')?.addEventListener('click', () => {
        ipcRenderer.send('window-close');
    });
});
