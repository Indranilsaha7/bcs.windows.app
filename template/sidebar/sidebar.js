window._renderSidebarImpl = function(activeMenu) {
    const menus = [
        { id: 'dashboard', href: 'dashboard.html', icon: '<path d="M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z"/>', text: 'Dashboard' },
        { id: 'products', href: 'all-product.html', icon: '<path d="M20 6h-4V4c0-1.11-.89-2-2-2h-4c-1.11 0-2 .89-2 2v2H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2zm-6 0h-4V4h4v2z"/>', text: 'Products' }
    ];
    let menuHtml = menus.map(m => \
        <a href="\" class="menu-item \">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor"><path d="\"/></svg> 
            \
        </a>
    \).join('');

    const SIDEBAR_HTML = \
    <aside class="sidebar">
        <div class="sidebar-header">
            <div style="background: rgba(0,0,0,0.3); border-radius: 8px; padding: 10px; display: flex; align-items: center; gap: 10px;">
                <svg viewBox="0 0 24 24" width="14" height="14" fill="#666"><path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/></svg>
                <input type="text" placeholder="Search menu..." style="background: transparent; border: none; color: #fff; font-size: 12px; outline: none; width: 100%;">
            </div>
        </div>
        <div class="sidebar-menu">
            \
        </div>
        <div class="sidebar-footer">
            <div class="user-profile mb-3">
                <div class="user-avatar" id="userAvatarText">U</div>
                <div class="user-info">
                    <div class="user-name" id="userName">User</div>
                    <div class="user-role" id="userRole">Role</div>
                </div>
            </div>
            <button id="logoutBtn" class="btn btn-outline-danger w-100" style="background: rgba(220,53,69,0.1); border: 1px solid rgba(220,53,69,0.2); color: #ff6b6b; font-weight: 600; font-size: 13px;" onclick="window.logout()">Logout</button>
        </div>
    </aside>
    \;

    const mainContent = document.querySelector('.main-content');
    if (mainContent) {
        mainContent.insertAdjacentHTML('beforebegin', SIDEBAR_HTML);
    } else {
        document.body.insertAdjacentHTML('afterbegin', SIDEBAR_HTML);
    }

    try {
        const sessionJson = localStorage.getItem('userSession');
        if (sessionJson) {
            const user = JSON.parse(sessionJson);
            const nameEl = document.getElementById('userName');
            const roleEl = document.getElementById('userRole');
            const avatarEl = document.getElementById('userAvatarText');
            if (nameEl) nameEl.textContent = user.name || user.username || 'User';
            if (roleEl) roleEl.textContent = (user.role || 'user').charAt(0).toUpperCase() + (user.role || 'user').slice(1);
            if (avatarEl) avatarEl.textContent = (user.name || user.username || 'U').charAt(0).toUpperCase();
        }
    } catch (e) {}
};
