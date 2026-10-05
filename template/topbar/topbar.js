const IDLE_TIMEOUT = 2 * 60 * 60 * 1000; 
function updateActivityTime() {
    if (localStorage.getItem('userSession')) {
        localStorage.setItem('lastActivity', Date.now().toString());
    }
}
function checkIdleTime() {
    if (!localStorage.getItem('userSession')) return;
    const lastActivity = parseInt(localStorage.getItem('lastActivity') || Date.now().toString());
    if (Date.now() - lastActivity >= IDLE_TIMEOUT) {
        window.logout('Your session expired due to 2 hours of inactivity.');
    }
}
window.logout = function(message) {
    localStorage.removeItem('userSession');
    localStorage.removeItem('lastActivity');
    if (message && typeof message === 'string') {
        sessionStorage.setItem('logoutMessage', message);
    }
    sessionStorage.clear();
    localStorage.clear();
    window.location.href = 'index.html';
};
if (localStorage.getItem('userSession')) {
    let throttleTimer;
    const activityHandler = () => {
        if (throttleTimer) return;
        throttleTimer = setTimeout(() => {
            updateActivityTime();
            throttleTimer = null;
        }, 5000);
    };
    ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'].forEach(event => {
        document.addEventListener(event, activityHandler, { passive: true });
    });
    updateActivityTime();
    setInterval(checkIdleTime, 60000);
}
function updateClock() {
    const clockEl = document.getElementById('tb-datetime');
    if (!clockEl) return;
    const now = new Date();
    const dateStr = now.toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' });
    const timeStr = now.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: true });
    clockEl.textContent = ` - `;
}
setInterval(updateClock, 1000);
document.addEventListener('DOMContentLoaded', updateClock);

window._renderTopbarImpl = function() {
    const TOPBAR_HTML = \
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
            <button class="tb-btn tb-btn-danger" id="logoutBtnTop" onclick="window.logout()"><svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><path d="M17 7l-1.41 1.41L18.17 11H8v2h10.17l-2.58 2.58L17 17l5-5zM4 5h8V3H4c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h8v-2H4V5z"/></svg> Logout</button>
        </div>
    </div>
</div>\;
    document.body.insertAdjacentHTML('afterbegin', TOPBAR_HTML);
    const sessionJson = localStorage.getItem('userSession');
    if (sessionJson) {
        const user = JSON.parse(sessionJson);
        const topbarUserEl = document.getElementById('nav-username-tb');
        if (topbarUserEl) topbarUserEl.textContent = user.username;
    }
    updateClock();
};
