// template/topbar/topbar.js

// 1. Session Idle Timeout Logic (2 Hours)
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
        // Session expired due to inactivity
        window.logout('Your session expired due to 2 hours of inactivity.');
    }
}

// Global Logout Function
window.logout = function(message) {
    localStorage.removeItem('userSession');
    localStorage.removeItem('lastActivity');
    
    if (message && typeof message === 'string') {
        sessionStorage.setItem('logoutMessage', message);
    }
    
    window.location.href = 'index.html';
};

// Start Activity Tracking Only If Logged In
if (localStorage.getItem('userSession')) {
    let throttleTimer;
    const activityHandler = () => {
        if (throttleTimer) return;
        throttleTimer = setTimeout(() => {
            updateActivityTime();
            throttleTimer = null;
        }, 5000); // Throttle saves to every 5 seconds
    };

    ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'].forEach(event => {
        document.addEventListener(event, activityHandler, { passive: true });
    });

    updateActivityTime();
    setInterval(checkIdleTime, 60000); // Check every minute
}

// 2. Real-time Clock for Topbar
function updateClock() {
    const clockEl = document.getElementById('tb-datetime');
    if (!clockEl) return;
    
    const now = new Date();
    const dateStr = now.toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' });
    const timeStr = now.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: true });
    
    clockEl.textContent = `${dateStr} - ${timeStr}`;
}

setInterval(updateClock, 1000);
document.addEventListener('DOMContentLoaded', updateClock);
