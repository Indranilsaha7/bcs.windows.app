window._renderTopbarImpl = function() {
    const TOPBAR_HTML = `
    <header class="topbar">
        <div>
            <h4 class="mb-0 fw-bold" style="background: linear-gradient(135deg, #6366f1, #a855f7); -webkit-background-clip: text; -webkit-text-fill-color: transparent;">BCS Enterprise</h4>
        </div>
        <div class="d-flex align-items-center gap-3">
            
            <!-- UPDATE NOTIFICATION BUTTON (Hidden by default) -->
            <button id="updateButton" class="btn btn-warning btn-sm fw-bold d-none" style="border-radius: 20px; animation: pulse 2s infinite;">
                <i class="bi bi-arrow-clockwise"></i> Update Available
            </button>

            <div class="user-profile">
                <div style="width: 36px; height: 36px; border-radius: 50%; background: linear-gradient(135deg, #6366f1, #a855f7); display: flex; align-items: center; justify-content: center; color: white; font-weight: bold;">
                    A
                </div>
                <div>
                    <div class="fw-semibold" style="font-size: 0.9rem; line-height: 1;">Admin</div>
                    <small class="text-muted" style="font-size: 0.75rem;">Administrator</small>
                </div>
            </div>
            <button class="btn logout-btn" onclick="if(window.logoutUser) window.logoutUser(); else window.location.href='index.html';">
                <i class="bi bi-box-arrow-right"></i> Logout
            </button>
        </div>
    </header>

    <style>
        @keyframes pulse {
            0% { box-shadow: 0 0 0 0 rgba(255, 193, 7, 0.7); }
            70% { box-shadow: 0 0 0 10px rgba(255, 193, 7, 0); }
            100% { box-shadow: 0 0 0 0 rgba(255, 193, 7, 0); }
        }
    </style>
    `;
    
    if (!document.querySelector('.topbar')) {
        document.body.insertAdjacentHTML('afterbegin', TOPBAR_HTML);
    }

    // --- AUTO UPDATER IPC LOGIC ---
    if (typeof require !== 'undefined') {
        const { ipcRenderer } = require('electron');
        const updateBtn = document.getElementById('updateButton');

        // Show button when an update is downloading
        ipcRenderer.on('update_available', () => {
            updateBtn.classList.remove('d-none');
            updateBtn.innerHTML = '<i class="bi bi-cloud-arrow-down"></i> Downloading Update...';
            updateBtn.disabled = true;
            updateBtn.classList.replace('btn-warning', 'btn-info');
        });

        // Show button when update is ready to install
        ipcRenderer.on('update_downloaded', () => {
            updateBtn.classList.remove('d-none');
            updateBtn.innerHTML = '<i class="bi bi-arrow-clockwise"></i> Restart to Update';
            updateBtn.disabled = false;
            updateBtn.classList.replace('btn-info', 'btn-success');
            
            updateBtn.onclick = () => {
                updateBtn.innerHTML = 'Restarting...';
                updateBtn.disabled = true;
                ipcRenderer.send('restart_app');
            };
        });
    }
};
