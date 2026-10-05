const TOPBAR_HTML = `
<div class="titlebar">
    <div class="titlebar-drag-region"></div>
    <div class="titlebar-content">
        <div class="titlebar-brand">
            <img src="al.png" alt="Logo" style="height: 24px; margin-right: 10px;">
            BCS Enterprise
        </div>
        <div class="topbar-actions">
            <button class="tb-btn tb-btn-info"><svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><path d="M19 4h-1V2h-2v2H8V2H6v2H5c-1.1 0-1.99.9-1.99 2L3 20a2 2 0 002 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V10h14v10z"/></svg> Calendar</button>
            <button class="tb-btn tb-btn-purple"><svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2z"/></svg> Chat</button>
            <button class="tb-btn tb-btn-grey"><svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><path d="M19 3h-4.18C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 0c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1zm-2 14l-4-4 1.41-1.41L10 14.17l6.59-6.59L18 9l-8 8z"/></svg> Tasks</button>
            <button class="tb-btn tb-btn-success"><svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zM7 10h2v7H7zm4-3h2v10h-2zm4 6h2v4h-2z"/></svg> Day Report</button>
            <button class="tb-btn tb-btn-yellow" onclick="window.location.href='pos.html'"><svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><path d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12.9-1.63h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49A1.003 1.003 0 0020 4H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2z"/></svg> POS</button>
            <span class="tb-text" id="tb-datetime"></span>
            <span class="tb-text">Welcome, <span class="tb-text-white" id="nav-username-tb">Admin</span></span>
            <button class="tb-btn tb-btn-danger" onclick="logout()"><svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><path d="M17 7l-1.41 1.41L18.17 11H8v2h10.17l-2.58 2.58L17 17l5-5zM4 5h8V3H4c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h8v-2H4V5z"/></svg> Logout</button>
        </div>
    </div>
</div>
`;

function getSidebarHTML(activeMenu) {
    const menus = [
        { id: 'dashboard', href: 'dashboard.html', icon: '<path d="M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z"/>', text: 'Dashboard' },
        { id: 'products', href: 'all-product.html', icon: '<path d="M20 6h-4V4c0-1.11-.89-2-2-2h-4c-1.11 0-2 .89-2 2v2H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2zm-6 0h-4V4h4v2z"/>', text: 'Products' }
    ];

    let menuHtml = menus.map(m => `
        <a href="${m.href}" class="menu-item ${activeMenu === m.id ? 'active' : ''}">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor"><path d="${m.icon}"/></svg> 
            ${m.text}
        </a>
    `).join('');

    return `
    <aside class="sidebar">
        <div class="sidebar-header">
            <div style="background: rgba(0,0,0,0.3); border-radius: 8px; padding: 10px; display: flex; align-items: center; gap: 10px;">
                <svg viewBox="0 0 24 24" width="14" height="14" fill="#666"><path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/></svg>
                <input type="text" placeholder="Search menu..." style="background: transparent; border: none; color: #fff; font-size: 12px; outline: none; width: 100%;">
            </div>
        </div>
        <div class="sidebar-menu">
            ${menuHtml}
        </div>
        <div class="sidebar-footer">
            <div class="user-profile mb-3">
                <div class="user-avatar" id="userAvatarText">U</div>
                <div class="user-info">
                    <div class="user-name" id="userName">User</div>
                    <div class="user-role" id="userRole">Role</div>
                </div>
            </div>
            <button id="logoutBtn" class="btn btn-outline-danger w-100" style="background: rgba(220,53,69,0.1); border: 1px solid rgba(220,53,69,0.2); color: #ff6b6b; font-weight: 600; font-size: 13px;">Logout</button>
        </div>
    </aside>
    `;
}

const FOOTER_HTML = `
<footer class="text-center py-4 mt-auto" style="color: #888899; font-size: 13px; z-index: 10;">
    &copy; 2026 Baroda Computer And Sales. All rights reserved.
</footer>
`;

function renderTopbar() {
    document.body.insertAdjacentHTML('afterbegin', TOPBAR_HTML);
    const sessionJson = localStorage.getItem('userSession');
    if (sessionJson) {
        const user = JSON.parse(sessionJson);
        const topbarUserEl = document.getElementById('nav-username-tb');
        if (topbarUserEl) topbarUserEl.textContent = user.username;
    }
}

function renderSidebar(activeMenu) {
      const mainContent = document.querySelector('.main-content');
      if (mainContent) {
          mainContent.insertAdjacentHTML('beforebegin', getSidebarHTML(activeMenu));
      } else {
          document.body.insertAdjacentHTML('afterbegin', getSidebarHTML(activeMenu));
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
}

function renderFooter() {
    const mainContent = document.querySelector('.main-content');
    if (mainContent) {
        mainContent.insertAdjacentHTML('beforeend', FOOTER_HTML);
    } else {
        document.body.insertAdjacentHTML('beforeend', FOOTER_HTML);
    }
}

window.renderTopbar = renderTopbar;
window.renderSidebar = renderSidebar;
window.renderFooter = renderFooter;

window.logout = function() {
    sessionStorage.clear();
    localStorage.clear();
    window.location.href = 'index.html';
};

document.addEventListener('click', e => { if (e.target && e.target.id === 'logoutBtn') { window.logout(); } });

