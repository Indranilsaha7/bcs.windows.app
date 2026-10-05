window._renderFooterImpl = function() {
    const FOOTER_HTML = \
    <footer class="text-center py-4 mt-auto" style="color: #888899; font-size: 13px; z-index: 10;">
        &copy; 2026 Baroda Computer And Sales. All rights reserved.
    </footer>
    \;

    const mainContent = document.querySelector('.main-content');
    if (mainContent) {
        mainContent.insertAdjacentHTML('beforeend', FOOTER_HTML);
    } else {
        document.body.insertAdjacentHTML('beforeend', FOOTER_HTML);
    }
};
