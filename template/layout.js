function loadCss(name) {
    if (!document.querySelector(`link[href*="template/${name}/${name}.css"]`)) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = `template/${name}/${name}.css`;
        document.head.appendChild(link);
    }
}

function loadScript(name) {
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src*="template/${name}/${name}.js"]`)) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.src = `template/${name}/${name}.js`;
        script.onload = resolve;
        script.onerror = reject;
        document.body.appendChild(script);
    });
}

window.renderTopbar = async function() {
    loadCss('topbar');
    await loadScript('topbar');
    if (window._renderTopbarImpl) window._renderTopbarImpl();
};

window.renderSidebar = async function(activeMenu, activeSubMenu = null) {
    loadCss('sidebar');
    await loadScript('sidebar');
    if (window._renderSidebarImpl) window._renderSidebarImpl(activeMenu, activeSubMenu);
};
