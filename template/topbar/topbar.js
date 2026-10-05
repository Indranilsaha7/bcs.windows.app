window._renderTopbarImpl = function() {
    const TOPBAR_HTML = `
    <header class="topbar">
        <div>
            <h4 class="mb-0 fw-bold" style="background: linear-gradient(135deg, #6366f1, #a855f7); -webkit-background-clip: text; -webkit-text-fill-color: transparent;">BCS Enterprise</h4>
        </div>
        <div class="d-flex align-items-center gap-3">
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
    </header>`;
    
    if (!document.querySelector('.topbar')) {
        document.body.insertAdjacentHTML('afterbegin', TOPBAR_HTML);
    }
};
