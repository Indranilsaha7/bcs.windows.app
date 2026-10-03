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
        { id: 'products', href: 'products.html', icon: '<path d="M20 6h-4V4c0-1.11-.89-2-2-2h-4c-1.11 0-2 .89-2 2v2H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2zm-6 0h-4V4h4v2z"/>', text: 'Products' },
        { id: 'pos', href: 'pos.html', icon: '<path d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12.9-1.63h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49A1.003 1.003 0 0020 4H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2z"/>', text: 'POS' },
        { id: 'settings', href: '#', icon: '<path d="M19.14 12.94c0.04-0.3 0.06-0.61 0.06-0.94c0-0.32-0.02-0.64-0.06-0.94l2.03-1.58c0.18-0.14 0.23-0.41 0.12-0.61l-1.92-3.32c-0.12-0.22-0.37-0.29-0.59-0.22l-2.39 0.96c-0.5-0.38-1.03-0.7-1.62-0.94L14.4 2.81c-0.04-0.24-0.24-0.41-0.48-0.41h-3.84c-0.24 0-0.43 0.17-0.47 0.41L9.25 5.35C8.66 5.59 8.12 5.92 7.63 6.29L5.24 5.33c-0.22-0.08-0.47 0-0.59 0.22L2.73 8.87C2.62 9.08 2.66 9.34 2.86 9.48l2.03 1.58c0.18 0.14 0.14 0.47 0.14 0.64s0.02 0.64 0.06 0.94l-2.03 1.58c-0.18 0.14-0.23 0.41-0.12 0.61l1.92 3.32c0.12 0.22 0.37 0.29 0.59 0.22l2.39-0.96c0.5 0.38 1.03 0.7 1.62 0.94l0.36 2.54c0.05 0.24 0.24 0.41 0.48 0.41h3.84c0.24 0 0.43-0.17 0.47-0.41l0.36-2.54c0.59-0.24 1.13-0.56 1.62-0.94l2.39 0.96c0.22 0.08 0.47 0 0.59-0.22l1.92-3.32c0.12-0.22 0.07-0.49-0.12-0.61L19.14 12.94zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6s3.6 1.62 3.6 3.6S13.98 15.6 12 15.6z"/>', text: 'Settings' },
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
            <h4 style="color:white; margin:0; font-size: 18px; font-weight:bold;">BCS Enterprise</h4>
        </div>
        <div class="sidebar-menu">
            ${menuHtml}
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
    
    // Set user info
    const sessionJson = localStorage.getItem('userSession');
    if (sessionJson) {
        const user = JSON.parse(sessionJson);
        const topbarUserEl = document.getElementById('nav-username-tb');
        if (topbarUserEl) topbarUserEl.textContent = user.username;
    }
}

function renderSidebar(activeMenu) {
    // Determine where to inject the sidebar. Usually inside body before main-content
    const mainContent = document.querySelector('.main-content');
    if (mainContent) {
        mainContent.insertAdjacentHTML('beforebegin', getSidebarHTML(activeMenu));
    } else {
        document.body.insertAdjacentHTML('afterbegin', getSidebarHTML(activeMenu));
    }
}

function renderFooter() {
    // Inject footer at the bottom of the main content
    const mainContent = document.querySelector('.main-content');
    if (mainContent) {
        mainContent.insertAdjacentHTML('beforeend', FOOTER_HTML);
    } else {
        document.body.insertAdjacentHTML('beforeend', FOOTER_HTML);
    }
}

// Export functions to global scope
window.renderTopbar = renderTopbar;
window.renderSidebar = renderSidebar;
window.renderFooter = renderFooter;
