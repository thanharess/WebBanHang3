// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

console.debug('site.js loaded');
// Chặn form mini-cart submit theo cách truyền thống (đề phòng Enter)
document.addEventListener('submit', function (e) {
    if (e.target && e.target.matches &&
        (e.target.matches('.mini-cart-update-form') || e.target.matches('.mini-cart-remove-form'))) {
        e.preventDefault();
        e.stopPropagation();
    }
}, true);
// ============================================================
// MINI-CART: Tăng / Giảm / Xóa sản phẩm
// ============================================================

function attachMiniCartHandlers() {
    // Tăng số lượng
    document.querySelectorAll('.mini-cart-inc').forEach(function (btn) {
        if (btn.dataset.bound === '1') return;
        btn.dataset.bound = '1';
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var form = btn.closest('.mini-cart-update-form');
            if (!form) return;
            var qtyInput = form.querySelector('.mini-cart-quantity-input');
            var qty = parseInt(qtyInput.value || '0', 10) + 1;
            qtyInput.value = qty;
            submitMiniCartUpdate(form);
        });
    });

    // Giảm số lượng
    document.querySelectorAll('.mini-cart-dec').forEach(function (btn) {
        if (btn.dataset.bound === '1') return;
        btn.dataset.bound = '1';
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var form = btn.closest('.mini-cart-update-form');
            if (!form) return;
            var qtyInput = form.querySelector('.mini-cart-quantity-input');
            var qty = Math.max(0, parseInt(qtyInput.value || '0', 10) - 1);
            qtyInput.value = qty;
            if (qty === 0) {
                var removeForm = form.parentElement.querySelector('.mini-cart-remove-form');
                if (removeForm) submitMiniCartRemove(removeForm);
                return;
            }
            submitMiniCartUpdate(form);
        });
    });

    // Nhập tay số lượng (ĐÃ DI CHUYỂN RA NGOÀI đúng chỗ)
    document.querySelectorAll('.mini-cart-update-form .mini-cart-qty-display').forEach(function (input) {
        if (input.dataset.bound === '1') return;
        input.dataset.bound = '1';

        // Khi rời ô hoặc Enter → gửi cập nhật
        input.addEventListener('change', function () {
            var form = input.closest('.mini-cart-update-form');
            if (!form) return;
            var val = parseInt(input.value, 10);
            if (isNaN(val) || val < 1) {
                input.value = 1;
                val = 1;
            }
            submitMiniCartUpdate(form);
        });

        // Chặn Enter submit form
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                e.stopPropagation();
                input.blur();
            }
        });
    });

    // Xóa sản phẩm
    document.querySelectorAll('.mini-cart-remove').forEach(function (btn) {
        if (btn.dataset.bound === '1') return;
        btn.dataset.bound = '1';
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var form = btn.closest('.mini-cart-remove-form');
            if (!form) return;
            submitMiniCartRemove(form);
        });
    });
}

function submitMiniCartUpdate(form) {
    var url = form.action;
    var formData = new FormData(form);
    var inc = form.querySelector('.mini-cart-inc');
    var dec = form.querySelector('.mini-cart-dec');
    if (inc) inc.disabled = true;
    if (dec) dec.disabled = true;

    fetch(url, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        body: formData,
        credentials: 'same-origin'
    })
        .then(function (r) { return r.json().catch(function () { return null; }); })
        .then(function (data) {
            if (!data) return;
            if (data.count !== undefined) {
                document.querySelectorAll('.cart-count').forEach(function (el) { el.textContent = data.count; });
            }
            if (data.itemId !== undefined) {
                var itemEl = document.querySelector('.mini-cart-item[data-product-id="' + data.itemId + '"]');
                if (itemEl) {
                    itemEl.querySelectorAll('.mini-cart-qty-display').forEach(function (qe) { qe.value = data.quantity; });
                    var qinput = itemEl.querySelector('.mini-cart-quantity-input');
                    if (qinput) qinput.value = data.quantity;
                }
            }
            if (data.cartTotal !== undefined) {
                var el = document.querySelector('.mini-cart-total-amount');
                if (el) el.textContent = Number(data.cartTotal).toLocaleString('vi-VN') + ' đ';
            }
        })
        .catch(function (err) { console.error('Update mini-cart failed', err); })
        .finally(function () {
            if (inc) inc.disabled = false;
            if (dec) dec.disabled = false;
        });
}

function submitMiniCartRemove(form) {
    var url = form.action;
    var formData = new FormData(form);
    var removeBtn = form.querySelector('.mini-cart-remove');
    if (removeBtn) removeBtn.disabled = true;

    fetch(url, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        body: formData,
        credentials: 'same-origin'
    })
        .then(function (r) { return r.json().catch(function () { return null; }); })
        .then(function (data) {
            if (!data) return;
            if (data.count !== undefined) {
                document.querySelectorAll('.cart-count').forEach(function (el) { el.textContent = data.count; });
            }
            if (data.cartTotal !== undefined) {
                var el = document.querySelector('.mini-cart-total-amount');
                if (el) el.textContent = Number(data.cartTotal).toLocaleString('vi-VN') + ' đ';
            }
            var pid = form.querySelector('input[name="productId"]').value;
            var itemEl = document.querySelector('.mini-cart-item[data-product-id="' + pid + '"]');
            if (itemEl) itemEl.remove();

            var remaining = document.querySelectorAll('.mini-cart-item').length;
            if (remaining === 0) {
                var container = document.querySelector('.mini-cart-items');
                if (container) {
                    container.innerHTML = '<div class="mini-cart-empty">Giỏ hàng đang trống.<br /><small>Hãy chọn sản phẩm bạn yêu thích.</small></div>';
                }
                var totalEl = document.querySelector('.mini-cart-total-amount');
                if (totalEl) totalEl.textContent = '0 đ';
            }
        })
        .catch(function (err) { console.error('Remove mini-cart failed', err); })
        .finally(function () {
            if (removeBtn) removeBtn.disabled = false;
        });
}

// ============================================================
// ADD-TO-CART qua AJAX
// ============================================================

document.addEventListener('submit', function (ev) {
    try {
        var form = ev.target;
        if (!form || !form.matches || !form.matches('form[data-ajax-add]')) return;
        ev.preventDefault();
        handleAjaxAdd(form);
    } catch (e) {
        console.error('Delegated submit handler error', e);
    }
}, true);

function handleAjaxAdd(form) {
    try {
        if (form.dataset.ajaxInflight === '1') return;
        form.dataset.ajaxInflight = '1';

        var submitBtn = form.querySelector('button[type="submit"]');
        if (submitBtn) { submitBtn.disabled = true; submitBtn.classList.add('is-loading'); }

        var url = form.action;
        var formData = new FormData(form);
        var addedProductId = formData.get('productId') || null;

        fetch(url, {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            body: formData,
            credentials: 'same-origin'
        })
            .then(function (r) { return r.json().catch(function () { return null; }); })
            .then(function (data) {
                if (data && data.count !== undefined) {
                    document.querySelectorAll('.cart-count').forEach(function (el) { el.textContent = data.count; });
                }
                return refreshMiniCartHtml().then(function () {
                    if (addedProductId) animateAddedItem(addedProductId.toString());
                    showAddToCartToast(form, data && data.message ? data.message : null);
                });
            })
            .catch(function (err) {
                console.error('Add-to-cart failed', err);
                updateMiniCartCount();
                refreshMiniCartHtml();
                showAddToCartToast(form, 'Lỗi khi thêm vào giỏ');
            })
            .finally(function () {
                form.dataset.ajaxInflight = '0';
                if (submitBtn) { submitBtn.disabled = false; submitBtn.classList.remove('is-loading'); }
            });
    } catch (e) {
        console.error('Add-to-cart handler error', e);
    }
}

function showAddToCartToast(form, message) {
    var toast = document.createElement('div');
    toast.className = 'toast align-items-center text-bg-success border-0 show';
    toast.style.position = 'fixed';
    toast.style.right = '1rem';
    toast.style.bottom = '1rem';
    toast.style.zIndex = '4000';
    toast.innerHTML = '<div class="d-flex"><div class="toast-body">' + (message || 'Đã thêm vào giỏ hàng') + '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
    document.body.appendChild(toast);
    setTimeout(function () { toast.remove(); }, 2500);
}

function refreshMiniCartHtml() {
    return fetch('/Cart/Dropdown', {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin'
    })
        .then(function (r) { return r.text(); })
        .then(function (html) {
            try {
                var parser = new DOMParser();
                var doc = parser.parseFromString(html, 'text/html');

                var oldMenu = document.querySelector('.dropdown-menu.mini-cart');
                if (!oldMenu) return;

                // Lấy nội dung mới từ partial view server trả về
                var newMenu = doc.querySelector('.dropdown-menu.mini-cart');

                if (newMenu) {
                    // Thay toàn bộ innerHTML (bao gồm title, items, total, actions)
                    oldMenu.innerHTML = newMenu.innerHTML;
                } else {
                    // Fallback nếu server trả về nội dung không có wrapper .mini-cart
                    // thì lấy thẳng toàn bộ body của response
                    var bodyContent = doc.body ? doc.body.innerHTML : html;
                    oldMenu.innerHTML = bodyContent;
                }

                // Gắn lại handler cho các nút vừa được thay mới
                attachMiniCartHandlers();
            } catch (err) {
                console.error('Failed to refresh mini-cart HTML', err);
            }
        })
        .catch(function (err) { console.error('refreshMiniCartHtml failed', err); });
}

function animateAddedItem(productId) {
    try {
        var itemEl = document.querySelector('.mini-cart-item[data-product-id="' + productId + '"]');
        if (!itemEl) return;
        itemEl.classList.remove('removed');
        itemEl.classList.add('added');
        setTimeout(function () { itemEl.classList.remove('added'); }, 1200);
    } catch (e) { }
}

function updateMiniCartCount() {
    fetch('/Cart/Count', {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin'
    })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data && data.count !== undefined) {
                document.querySelectorAll('.cart-count').forEach(function (el) { el.textContent = data.count; });
            }
        })
        .catch(function () { });
}

// ============================================================
// DROPDOWN: Ngăn chặn tự đóng khi bấm vào bên trong giỏ hàng
// ============================================================

document.addEventListener('DOMContentLoaded', function () {
    // Chặn sự kiện click bên trong mini-cart lan ra ngoài
    // (Bootstrap dùng sự kiện này để tự đóng dropdown)
    document.querySelectorAll('.dropdown-menu.mini-cart').forEach(function (menu) {
        menu.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    });

    // Gắn handler cho các nút mini-cart đã render sẵn
    attachMiniCartHandlers();
});

// ============================================================
// COOKIE CONSENT BANNER
// ============================================================

(function () {
    function getCookie(name) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + name.replace(/([.$?*|{}()[\]\\/+^])/g, '\\$1') + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : null;
    }

    function hideBanner(banner) {
        banner.classList.add('d-none');
        banner.classList.remove('cookie-banner--visible');
        banner.setAttribute('aria-hidden', 'true');
        banner.style.display = 'none';
    }

    function showBanner(banner) {
        banner.classList.remove('d-none');
        banner.classList.add('cookie-banner--visible');
        banner.setAttribute('aria-hidden', 'false');
        banner.style.display = 'block';
    }

    function initCookieBanner() {
        var banner = document.getElementById('cookieBanner');
        if (!banner) return;

        // Chỉ ẩn khi đã Đồng ý (cookieConsent=1)
        if (getCookie('cookieConsent') === '1') {
            hideBanner(banner);
        } else {
            showBanner(banner);
        }

        var acceptBtn = document.getElementById('acceptCookies');
        if (acceptBtn) {
            acceptBtn.addEventListener('click', function () {
                var expires = new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toUTCString();
                document.cookie = 'cookieConsent=1; path=/; expires=' + expires + '; SameSite=Lax';
                hideBanner(banner);
            });
        }

        var dismissBtn = document.getElementById('dismissCookieBanner');
        if (dismissBtn) {
            dismissBtn.addEventListener('click', function () {
                // Ẩn tạm phiên này (không set cookie = lần sau vẫn hiện)
                hideBanner(banner);
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCookieBanner);
    } else {
        initCookieBanner();
    }
})();

                // ============================================================
// FIX DROPDOWN + SUBMENU: hover hiện, rời chuột thì ẩn
// ============================================================
document.addEventListener('DOMContentLoaded', function () {
    // Submenu cấp 2
    document.querySelectorAll('.category-dropdown .dropdown-submenu').forEach(function (submenu) {
        var parentDropdown = submenu.closest('.dropdown-menu');

        submenu.addEventListener('mouseenter', function () {
            if (parentDropdown) parentDropdown.classList.add('show');
            var sub = submenu.querySelector('.dropdown-menu');
            if (sub) sub.classList.add('show');
        });

        submenu.addEventListener('mouseleave', function () {
            var sub = submenu.querySelector('.dropdown-menu');
            if (sub) sub.classList.remove('show');
        });
    });

    // Dropdown cấp 1 (Nam, Nữ, Danh mục...): ẩn khi rời chuột (không áp dụng mini-cart)
    document.querySelectorAll('.shop-header .nav-item.dropdown').forEach(function (item) {
        item.addEventListener('mouseleave', function () {
            item.querySelectorAll('.dropdown-submenu > .dropdown-menu.show').forEach(function (sub) {
                sub.classList.remove('show');
            });
            var menu = item.querySelector(':scope > .dropdown-menu:not(.mini-cart)');
            if (menu) {
                menu.classList.remove('show');
                var toggle = item.querySelector('[data-bs-toggle="dropdown"]');
                if (toggle) toggle.setAttribute('aria-expanded', 'false');
            }
        });
        item.addEventListener('mouseenter', function () {
            var menu = item.querySelector(':scope > .dropdown-menu:not(.mini-cart)');
            if (menu && window.matchMedia('(min-width: 992px)').matches) {
                menu.classList.add('show');
                var toggle = item.querySelector('[data-bs-toggle="dropdown"]');
                if (toggle) toggle.setAttribute('aria-expanded', 'true');
            }
        });
    });
});
// ============================================================
// HEADER AUTO-HIDE — Chỉ kích hoạt khi zoom > 150%
// Không ẩn khi có dropdown đang mở
// ============================================================
(function () {
    var header = document.querySelector('.site-header');
    if (!header) return;

    var ZOOM_THRESHOLD = 1.5; // > 150% mới áp dụng
    var BASE_PERCENT = 0.4;   // ngưỡng ẩn ở zoom 150%

    function getZoomRatio() {
        var ratio = window.outerWidth / window.innerWidth;
        return Math.max(1, Math.round(ratio * 100) / 100);
    }

    function hasOpenDropdown() {
        return header.querySelector('.dropdown-menu.show') !== null;
    }

    function updateHeader() {
        var zoom = getZoomRatio();

        // Zoom ≤ 150% → luôn hiện
        if (zoom <= ZOOM_THRESHOLD) {
            header.classList.remove('header-hidden');
            return;
        }

        // Có dropdown đang mở → không ẩn
        if (hasOpenDropdown()) {
            header.classList.remove('header-hidden');
            return;
        }

        var y = window.pageYOffset || document.documentElement.scrollTop;
        // Tỉ lệ nghịch với zoom (zoom càng cao càng ẩn sớm)
        var percent = BASE_PERCENT / zoom;
        var threshold = window.innerHeight * percent;

        if (y > threshold) {
            header.classList.add('header-hidden');
        } else {
            header.classList.remove('header-hidden');
        }
    }

    window.addEventListener('scroll', updateHeader, { passive: true });
    window.addEventListener('resize', updateHeader);
    document.addEventListener('DOMContentLoaded', updateHeader);

    // Khi dropdown mở/đóng → cập nhật ngay
    document.addEventListener('shown.bs.dropdown', updateHeader);
    document.addEventListener('hidden.bs.dropdown', updateHeader);

    document.addEventListener('click', function () {
        setTimeout(updateHeader, 50);
    });

    updateHeader();
})();
// Nút liên hệ nổi: 1 nút mở 2 (Zalo + copy SĐT)
document.addEventListener('DOMContentLoaded', function () {
    var root = document.getElementById('contactFloat');
    var toggle = document.getElementById('contactFloatToggle');
    var actions = document.getElementById('contactFloatActions');
    var copyBtn = document.getElementById('copyPhoneBtn');
    var toast = document.getElementById('contactFloatToast');
    if (!toggle || !actions) return;

    function openActions() {
        actions.removeAttribute('hidden');
        toggle.classList.add('is-open');
        toggle.setAttribute('aria-expanded', 'true');
    }
    function closeActions() {
        actions.setAttribute('hidden', '');
        toggle.classList.remove('is-open');
        toggle.setAttribute('aria-expanded', 'false');
    }

    // Hover hiện / rời ẩn; click vẫn bật/tắt (mobile)
    if (root) {
        root.addEventListener('mouseenter', openActions);
        root.addEventListener('mouseleave', closeActions);
    }
    toggle.addEventListener('click', function (e) {
        e.preventDefault();
        if (actions.hasAttribute('hidden')) openActions();
        else closeActions();
    });

    if (copyBtn) {
        copyBtn.addEventListener('click', function () {
            var phone = copyBtn.getAttribute('data-phone') || '';
            function showToast() {
                if (!toast) return;
                toast.hidden = false;
                setTimeout(function () { toast.hidden = true; }, 1800);
            }
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(phone).then(showToast).catch(function () {
                    window.prompt('Sao chép số điện thoại:', phone);
                });
            } else {
                window.prompt('Sao chép số điện thoại:', phone);
            }
        });
    }
});


// ============================================================
// LIVE CHAT (poll) — mỗi khách một phiên riêng
// ============================================================
(function () {
    var toggle = document.getElementById('liveChatToggle');
    var panel = document.getElementById('liveChatPanel');
    var closeBtn = document.getElementById('liveChatClose');
    var form = document.getElementById('liveChatForm');
    var box = document.getElementById('liveChatMessages');
    var input = document.getElementById('liveChatInput');
    var nameInput = document.getElementById('liveChatName');
    var genderInput = document.getElementById('liveChatGender');
    if (!toggle || !panel || !form || !box) return;

    var lastId = 0;
    var timer = null;
    var greeted = false;

    function getToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function appendMsg(m, force) {
        if (!m || !m.id) return;
        if (!force && m.id <= lastId) return;
        lastId = Math.max(lastId, m.id || 0);
        var wrap = document.createElement('div');
        wrap.className = 'live-chat-row ' + (m.isFromStaff ? 'staff' : 'guest');
        var div = document.createElement('div');
        div.className = 'live-chat-bubble ' + (m.isFromStaff ? 'staff' : 'guest');
        var text = document.createElement('div');
        text.textContent = m.content || '';
        var meta = document.createElement('span');
        meta.className = 'meta';
        meta.textContent = (m.isFromStaff ? 'CH&T GOLF' : 'Bạn') + (m.at ? ' · ' + m.at : '');
        div.appendChild(text);
        div.appendChild(meta);
        wrap.appendChild(div);
        box.appendChild(wrap);
        box.scrollTop = box.scrollHeight;
    }

    function showGreeting() {
        if (greeted || box.querySelector('.live-chat-bubble')) return;
        greeted = true;
        var wrap = document.createElement('div');
        wrap.className = 'live-chat-row staff';
        var div = document.createElement('div');
        div.className = 'live-chat-bubble staff';
        div.innerHTML = '<div>Xin chào! CH&T GOLF sẵn sàng hỗ trợ bạn. Hãy để lại câu hỏi nhé.</div><span class="meta">CH&T GOLF</span>';
        wrap.appendChild(div);
        box.appendChild(wrap);
    }

    function poll() {
        fetch('/Chat/Poll?afterId=' + lastId, { credentials: 'same-origin' })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                var list = data.messages || [];
                if (list.length === 0 && lastId === 0) showGreeting();
                list.forEach(function (m) {
                    appendMsg({
                        id: m.id || m.Id,
                        content: m.content || m.Content,
                        isFromStaff: m.isFromStaff === true || m.IsFromStaff === true,
                        at: m.at || m.At
                    });
                });
            })
            .catch(function () {});
    }

    function openChat() {
        panel.removeAttribute('hidden');
        if (!timer) {
            poll();
            timer = setInterval(poll, 3000);
        } else if (lastId === 0) {
            showGreeting();
        }
    }
    function closeChat() {
        panel.setAttribute('hidden', '');
    }

    toggle.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        if (panel.hasAttribute('hidden')) openChat();
        else closeChat();
    });
    if (closeBtn) closeBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        closeChat();
    });
    panel.addEventListener('click', function (e) { e.stopPropagation(); });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        var text = (input.value || '').trim();
        if (!text) return;
        var body = new URLSearchParams();
        body.set('content', text);
        body.set('guestName', (nameInput && nameInput.value) || '');
        body.set('gender', (genderInput && genderInput.value) || '');
        var token = getToken();
        if (token) body.set('__RequestVerificationToken', token);

        fetch('/Chat/Send', {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: body.toString()
        })
            .then(function (r) { return r.json(); })
            .then(function (m) {
                appendMsg({
                    id: m.id || m.Id,
                    content: m.content || m.Content,
                    isFromStaff: false,
                    at: m.at || m.At
                }, true);
                input.value = '';
            })
            .catch(function () { alert('Không gửi được tin nhắn. Thử lại.'); });
    });
})();
