// ===== 3H AUTO - giỏ hàng đơn giản, lưu trên trình duyệt (localStorage) =====
(function () {
    var KEY = '3hauto-cart';
    var money = new Intl.NumberFormat('vi-VN');

    function load() {
        try { return JSON.parse(localStorage.getItem(KEY)) || []; } catch (e) { return []; }
    }
    function save(items) {
        try { localStorage.setItem(KEY, JSON.stringify(items)); } catch (e) { /* bỏ qua */ }
        render();
    }
    function esc(s) {
        return String(s || '').replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function add(item) {
        var items = load();
        var found = items.find(function (x) { return x.id === item.id; });
        if (found) {
            found.qty = Math.min(found.qty + item.qty, item.max || 999);
        } else {
            items.push(item);
        }
        save(items);
        showToast('Đã thêm "' + item.name + '" vào giỏ');
    }

    function render() {
        var items = load();
        var count = items.reduce(function (s, x) { return s + x.qty; }, 0);
        var total = items.reduce(function (s, x) { return s + x.qty * x.price; }, 0);

        document.querySelectorAll('[data-cart-count]').forEach(function (el) {
            el.textContent = count;
            el.classList.toggle('d-none', count === 0);
        });
        document.querySelectorAll('[data-cart-total]').forEach(function (el) {
            el.textContent = money.format(total) + ' ₫';
        });
        document.querySelectorAll('[data-cart-checkout]').forEach(function (el) {
            el.classList.toggle('disabled', count === 0);
        });
        renderCheckout(items, total);

        var box = document.querySelector('[data-cart-items]');
        if (!box) return;
        if (items.length === 0) {
            box.innerHTML = '<div class="cart-empty"><i class="bi bi-cart-x"></i><p>Giỏ hàng đang trống</p></div>';
            return;
        }
        box.innerHTML = items.map(function (x) {
            var img = x.img
                ? '<img src="' + esc(x.img) + '" alt="" />'
                : '<i class="bi bi-box-seam"></i>';
            return '<div class="cart-item" data-id="' + x.id + '">' +
                '<div class="cart-thumb">' + img + '</div>' +
                '<div class="flex-grow-1">' +
                '<a class="cart-name" href="/Shop/Detail/' + x.id + '">' + esc(x.name) + '</a>' +
                '<div class="text-danger fw-semibold">' + money.format(x.price) + ' ₫</div>' +
                '<div class="cart-qty">' +
                '<button type="button" data-qty="-1" aria-label="Giảm">−</button>' +
                '<span>' + x.qty + '</span>' +
                '<button type="button" data-qty="1" aria-label="Tăng">+</button>' +
                '</div></div>' +
                '<button type="button" class="btn btn-sm btn-link text-muted" data-remove aria-label="Xóa"><i class="bi bi-trash"></i></button>' +
                '</div>';
        }).join('');
    }

    // ----- Trang Đặt hàng: hiện tóm tắt giỏ, gửi giỏ kèm form -----
    function renderCheckout(items, total) {
        var form = document.querySelector('[data-checkout-form]');
        if (!form) return;
        var empty = document.querySelector('[data-checkout-empty]');
        if (empty) empty.classList.toggle('d-none', items.length > 0);
        form.classList.toggle('d-none', items.length === 0);

        var box = form.querySelector('[data-checkout-items]');
        box.innerHTML = items.map(function (x) {
            var img = x.img ? '<img src="' + esc(x.img) + '" alt="" />' : '<i class="bi bi-box-seam"></i>';
            return '<div class="checkout-line">' +
                '<div class="order-thumb">' + img + '<span class="checkout-qty">' + x.qty + '</span></div>' +
                '<div class="flex-grow-1 small fw-semibold">' + esc(x.name) + '</div>' +
                '<div class="text-nowrap small">' + money.format(x.qty * x.price) + ' ₫</div>' +
                '</div>';
        }).join('');
        form.querySelectorAll('[data-checkout-total]').forEach(function (el) {
            el.textContent = money.format(total) + ' ₫';
        });
        form.querySelector('[data-checkout-cart]').value = JSON.stringify(items.map(function (x) {
            return { productId: x.id, quantity: x.qty };
        }));
    }

    function showToast(text) {
        var el = document.getElementById('cartToast');
        if (!el || !window.bootstrap) return;
        el.querySelector('[data-toast-text]').textContent = text;
        bootstrap.Toast.getOrCreateInstance(el, { delay: 2000 }).show();
    }

    // Thêm nhiều sản phẩm một lần (combo bảo dưỡng)
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-add-combo]');
        if (!btn) return;
        var list = [];
        try { list = JSON.parse(btn.dataset.items || '[]'); } catch (err) { list = []; }
        if (!list.length) return;
        var items = load();
        list.forEach(function (it) {
            var found = items.find(function (x) { return x.id === it.id; });
            if (found) found.qty = Math.min(found.qty + 1, it.max || 999);
            else items.push({ id: it.id, name: it.name, price: it.price, img: it.img || '', max: it.max || 999, qty: 1 });
        });
        save(items);
        showToast('Đã thêm ' + list.length + ' sản phẩm của combo vào giỏ');
    });

    document.addEventListener('click', function (e) {
        var addBtn = e.target.closest('[data-add-to-cart]');
        if (addBtn) {
            var qtyInput = document.querySelector(addBtn.getAttribute('data-qty-input') || '#__none');
            add({
                id: parseInt(addBtn.dataset.id, 10),
                name: addBtn.dataset.name,
                price: parseFloat(addBtn.dataset.price),
                img: addBtn.dataset.img || '',
                max: parseInt(addBtn.dataset.max, 10) || 999,
                qty: qtyInput ? Math.max(1, parseInt(qtyInput.value, 10) || 1) : 1
            });
            return;
        }

        var row = e.target.closest('.cart-item');
        if (!row) {
            if (e.target.closest('[data-cart-clear]')) save([]);
            return;
        }
        var id = parseInt(row.dataset.id, 10);
        var items = load();
        var item = items.find(function (x) { return x.id === id; });
        if (!item) return;

        var qtyBtn = e.target.closest('[data-qty]');
        if (qtyBtn) {
            item.qty += parseInt(qtyBtn.dataset.qty, 10);
            if (item.max) item.qty = Math.min(item.qty, item.max);
            if (item.qty <= 0) items = items.filter(function (x) { return x.id !== id; });
            save(items);
        } else if (e.target.closest('[data-remove]')) {
            save(items.filter(function (x) { return x.id !== id; }));
        }
    });

    // Nút +/- số lượng ở trang chi tiết
    document.addEventListener('click', function (e) {
        var step = e.target.closest('[data-step]');
        if (!step) return;
        var input = document.querySelector(step.dataset.target);
        var max = parseInt(input.max, 10) || 999;
        var v = (parseInt(input.value, 10) || 1) + parseInt(step.dataset.step, 10);
        input.value = Math.min(Math.max(1, v), max);
    });

    // Đặt hàng thành công -> xóa giỏ
    if (document.querySelector('[data-clear-cart]')) {
        try { localStorage.removeItem(KEY); } catch (e) { /* bỏ qua */ }
    }

    render();
})();
