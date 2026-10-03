const { ipcRenderer } = require('electron');

// Custom Titlebar Controls
document.getElementById('btn-minimize').addEventListener('click', () => {
    ipcRenderer.send('window-minimize');
});

document.getElementById('btn-maximize').addEventListener('click', () => {
    ipcRenderer.send('window-maximize');
});

document.getElementById('btn-close').addEventListener('click', () => {
    ipcRenderer.send('window-close');
});

// Login Logic
document.getElementById('btn-login').addEventListener('click', () => {
    const username = document.getElementById('username').value.trim();
    const password = document.getElementById('password').value;
    const code = document.getElementById('code').value;
    const statusEl = document.getElementById('status-message');

    if (!username || !password || !code) {
        statusEl.textContent = 'Username, password, and security code are required.';
        statusEl.className = 'status-message text-error';
        return;
    }

    // Simulate API Call
    statusEl.textContent = 'Authenticating...';
    statusEl.className = 'status-message text-success';
    document.getElementById('btn-login').disabled = true;

    setTimeout(() => {
        statusEl.textContent = `Welcome back, ${username}! (Electron UI Active)`;
        // Here we would normally navigate to the Dashboard HTML
        setTimeout(() => {
            document.getElementById('btn-login').disabled = false;
        }, 2000);
    }, 1500);
});
