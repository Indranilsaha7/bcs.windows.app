function loadCss(name) {
    if (!document.querySelector(\link[href*="template/\/\.css"]\)) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = \	emplate/\/\.css\;
        document.head.appendChild(link);
    }
}

function loadScript(name) {
    return new Promise((resolve, reject) => {
        if (document.querySelector(\script[src*="template/\/\.js"]\)) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.src = \	emplate/\/\.js\;
        script.onload = resolve;
        script.onerror = reject;
        document.body.appendChild(script);
    });
}

window.renderTopbar = async function() {
    loadCss('topbar');
    await loadScript('topbar');
    if (typeof window._renderTopbarImpl === 'function') {
        window._renderTopbarImpl();
    }
};

window.renderSidebar = async function(activeMenu) {
    loadCss('sidebar');
    await loadScript('sidebar');
    if (typeof window._renderSidebarImpl === 'function') {
        window._renderSidebarImpl(activeMenu);
    }
};

window.renderFooter = async function() {
    loadCss('footer');
    await loadScript('footer');
    if (typeof window._renderFooterImpl === 'function') {
        window._renderFooterImpl();
    }
};

window.logout = function() {
    sessionStorage.clear();
    localStorage.clear();
    window.location.href = 'index.html';
};
