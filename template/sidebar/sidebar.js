window._renderSidebarImpl = function(activeMenu, activeSubMenu = null) {
    const SIDEBAR_HTML = `
    <aside class="sidebar">
        <div class="sidebar-header">
            <h5 class="mb-0 fw-bold text-white d-flex align-items-center gap-2">
                <i class="bi bi-box-seam" style="color: #6366f1;"></i> BCS POS
            </h5>
        </div>
        <nav class="sidebar-menu">
            <a href="dashboard.html" class="menu-item ${activeMenu === 'dashboard' ? 'active' : ''}">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor"><path d="M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z"/></svg> 
                Dashboard
            </a>
            
            <div class="menu-group">
                <a href="#" class="menu-item ${activeMenu === 'products' ? 'active' : ''}" data-bs-toggle="collapse" data-bs-target="#productsMenu" aria-expanded="${activeMenu === 'products' ? 'true' : 'false'}">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor"><path d="M20 6h-4V4c0-1.11-.89-2-2-2h-4c-1.11 0-2 .89-2 2v2H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2zm-6 0h-4V4h4v2z"/></svg> 
                    Products
                    <i class="bi bi-chevron-down ms-auto" style="font-size: 0.8rem;"></i>
                </a>
                <div class="collapse ${activeMenu === 'products' ? 'show' : ''}" id="productsMenu">
                    <div class="submenu">
                        <a href="all-product.html" class="submenu-item ${activeSubMenu === 'all-product' ? 'active' : ''}">All Products</a>
                        <a href="add-product.html" class="submenu-item ${activeSubMenu === 'add-product' ? 'active' : ''}">Add Product</a>
                    </div>
                </div>
            </div>

            <a href="pos.html" class="menu-item ${activeMenu === 'pos' ? 'active' : ''}">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor"><path d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12.9-1.63h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49c.08-.14.12-.31.12-.48 0-.55-.45-1-1-1H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2z"/></svg> 
                POS System
            </a>
        </nav>
    </aside>
    <style>
        .submenu {
            padding-left: 2rem;
            display: flex;
            flex-direction: column;
            gap: 4px;
            margin-top: 4px;
        }
        .submenu-item {
            color: #9ca3af;
            text-decoration: none;
            padding: 8px 12px;
            border-radius: 8px;
            font-size: 0.9rem;
            transition: all 0.2s ease;
        }
        .submenu-item:hover, .submenu-item.active {
            color: #fff;
            background: rgba(255, 255, 255, 0.1);
        }
        .menu-item[aria-expanded="true"] .bi-chevron-down {
            transform: rotate(180deg);
        }
        .bi-chevron-down {
            transition: transform 0.2s ease;
        }
    </style>`;
    
    if (!document.querySelector('.sidebar')) {
        document.body.insertAdjacentHTML('afterbegin', SIDEBAR_HTML);
    }
};
