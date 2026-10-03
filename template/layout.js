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
        { id: 'products', href: 'products.html', icon: '<path d="M20 6h-4V4c0-1.11-.89-2-2-2h-4c-1.11 0-2 .89-2 2v2H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2zm-6 0h-4V4h4v2z"/>', text: 'Products' },
        { id: 'print-label', href: '#', icon: '<path d="M19 8H5c-1.66 0-3 1.34-3 3v6h4v4h12v-4h4v-6c0-1.66-1.34-3-3-3zm-3 11H8v-5h8v5zm3-7c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm-1-9H6v4h12V3z"/>', text: 'Print Label' },
        { id: 'mail', href: '#', icon: '<path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/>', text: 'Mail & Notifications' },
        { id: 'purchase', href: '#', icon: '<path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14z"/><path d="M7 12h2v5H7zm4-3h2v8h-2zm4-3h2v11h-2z"/>', text: 'Purchase' },
        { id: 'sales', href: '#', icon: '<path d="M21 5c-1.11-.35-2.33-.5-3.5-.5-1.95 0-4.05.4-5.5 1.5-1.45-1.1-3.55-1.5-5.5-1.5S2.45 4.9 1 6v14.65c0 .25.25.5.5.5.1 0 .15-.05.25-.05C3.1 20.45 5.05 20 6.5 20c1.95 0 4.05.4 5.5 1.5 1.35-.85 3.8-1.5 5.5-1.5 1.65 0 3.35.3 4.75 1.05.1.05.15.05.25.05.25 0 .5-.25.5-.5V6c-.6-.45-1.25-.75-2-1zM21 18.5c-1.1-.35-2.3-.5-3.5-.5-1.7 0-4.15.65-5.5 1.5V8c1.35-.85 3.8-1.5 5.5-1.5 1.2 0 2.4.15 3.5.5v11.5z"/>', text: 'Sales' },
        { id: 'people', href: '#', icon: '<path d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"/>', text: 'People' },
        { id: 'reports', href: '#', icon: '<path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4z"/>', text: 'Reports' },
        { id: 'leaderboard', href: '#', icon: '<path d="M19 5h-2V3c0-1.1-.9-2-2-2H9c-1.1 0-2 .9-2 2v2H5c-1.1 0-2 .9-2 2v1c0 2.55 1.92 4.63 4.39 4.94.63 1.5 1.98 2.63 3.61 2.96V19H7v2h10v-2h-4v-1.1c1.63-.33 2.98-1.46 3.61-2.96C19.08 14.63 21 12.55 21 10V7c0-1.1-.9-2-2-2zM5 10V7h2v3.82C5.84 10.4 5 9.3 5 10zm14 0c0-.7-.16-1.4-.42-2H17V7h2v3c0-1.1-.84-2-2-2zM9 3h6v12c0 1.66-1.34 3-3 3s-3-1.34-3-3V3z"/>', text: 'Leaderboard' },
        { id: 'employee', href: '#', icon: '<path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>', text: 'Employee Management' },
        { id: 'security', href: '#', icon: '<path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm0 10.99h7c-.53 4.12-3.28 7.79-7 8.94V12H5V6.3l7-3.11v8.8z"/>', text: 'Security & IP' },
        { id: 'settings', href: '#', icon: '<path d="M19.14 12.94c0.04-0.3 0.06-0.61 0.06-0.94c0-0.32-0.02-0.64-0.06-0.94l2.03-1.58c0.18-0.14 0.23-0.41 0.12-0.61l-1.92-3.32c-0.12-0.22-0.37-0.29-0.59-0.22l-2.39 0.96c-0.5-0.38-1.03-0.7-1.62-0.94L14.4 2.81c-0.04-0.24-0.24-0.41-0.48-0.41h-3.84c-0.24 0-0.43 0.17-0.47 0.41L9.25 5.35C8.66 5.59 8.12 5.92 7.63 6.29L5.24 5.33c-0.22-0.08-0.47 0-0.59 0.22L2.73 8.87C2.62 9.08 2.66 9.34 2.86 9.48l2.03 1.58c0.18 0.14 0.14 0.47 0.14 0.64s0.02 0.64 0.06 0.94l-2.03 1.58c-0.18 0.14-0.23 0.41-0.12 0.61l1.92 3.32c0.12 0.22 0.37 0.29 0.59 0.22l2.39-0.96c0.5 0.38 1.03 0.7 1.62 0.94l0.36 2.54c0.05 0.24 0.24 0.41 0.48 0.41h3.84c0.24 0 0.43-0.17 0.47-0.41l0.36-2.54c0.59-0.24 1.13-0.56 1.62-0.94l2.39 0.96c0.22 0.08 0.47 0 0.59-0.22l1.92-3.32c0.12-0.22 0.07-0.49-0.12-0.61L19.14 12.94zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6s3.6 1.62 3.6 3.6S13.98 15.6 12 15.6z"/>', text: 'Settings' },
        { id: 'account', href: '#', icon: '<path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z"/>', text: 'Account' }
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
