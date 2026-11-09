// ===== 3H AUTO - khung chat trợ lý hỏi đáp =====
(function () {
    var root = document.querySelector('[data-chat]');
    if (!root) return;

    var KEY = '3hauto-chat';
    var panel = root.querySelector('[data-chat-panel]');
    var body = root.querySelector('[data-chat-body]');
    var form = root.querySelector('[data-chat-form]');
    var input = root.querySelector('[data-chat-text]');
    var suggest = root.querySelector('[data-chat-suggest]');
    var token = root.querySelector('input[name="__RequestVerificationToken"]').value;
    var askUrl = root.dataset.askUrl;
    var imageUrl = root.dataset.imageUrl;
    var fileInput = root.querySelector('[data-chat-file]');
    var money = new Intl.NumberFormat('vi-VN');
    var busy = false;

    var WELCOME = 'Xin chào! Mình là trợ lý của 3H AUTO. Bạn cần tìm phụ tùng gì, cho dòng xe nào?';

    function load() {
        try { return JSON.parse(sessionStorage.getItem(KEY)) || []; } catch (e) { return []; }
    }
    function save(list) {
        try { sessionStorage.setItem(KEY, JSON.stringify(list.slice(-20))); } catch (e) { /* bỏ qua */ }
    }
    var history = load(); // [{role, content, products?}]

    function esc(s) {
        return String(s || '').replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function productHtml(p) {
        var img = p.imageUrl ? '<img src="' + esc(p.imageUrl) + '" alt="" />' : '<i class="bi bi-box-seam"></i>';
        var stock = p.stock <= 0 ? '<span class="text-secondary">Hết hàng</span>'
            : p.stock <= 5 ? '<span class="text-warning">Còn ' + p.stock + '</span>'
            : '<span class="text-success">Còn hàng</span>';
        return '<a class="chat-product" href="' + esc(p.url) + '">' +
            '<span class="chat-product-img">' + img + '</span>' +
            '<span class="flex-grow-1"><b>' + esc(p.name) + '</b>' +
            '<small>Mã ' + esc(p.partNumber) + ' · ' + stock + '</small></span>' +
            '<span class="chat-product-price">' + money.format(p.price) + ' ₫</span></a>';
    }

    function bubble(m) {
        var html = '<div class="chat-msg ' + (m.role === 'user' ? 'me' : 'bot') + '">';
        if (m.img) html += '<img class="chat-img" src="' + esc(m.img) + '" alt="Ảnh đã gửi" />';
        if (m.text !== '') html += '<div class="chat-bubble">' + esc(m.text != null ? m.text : m.content).replace(/\n/g, '<br>') + '</div>';
        if (m.products && m.products.length) {
            html += '<div class="chat-products">' + m.products.map(productHtml).join('') + '</div>';
        }
        return html + '</div>';
    }

    function render() {
        body.innerHTML = bubble({ role: 'assistant', content: WELCOME }) + history.map(bubble).join('') +
            (busy ? '<div class="chat-msg bot"><div class="chat-bubble chat-typing"><span></span><span></span><span></span></div></div>' : '');
        suggest.hidden = history.length > 0;
        body.scrollTop = body.scrollHeight;
    }

    function ask(text) {
        text = (text || '').trim();
        if (!text || busy) return;
        history.push({ role: 'user', content: text });
        busy = true;
        render();

        fetch(askUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify({
                messages: history.map(function (m) { return { role: m.role, content: m.content }; })
            })
        })
            .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
            .then(function (data) {
                history.push({ role: 'assistant', content: data.reply, products: data.products });
            })
            .catch(function () {
                history.push({ role: 'assistant', content: 'Xin lỗi, hiện mình chưa trả lời được. Bạn thử lại sau ít phút nhé.' });
            })
            .finally(function () {
                busy = false;
                save(history);
                render();
                input.focus();
            });
    }

    // ----- Gửi ảnh: thu nhỏ trên trình duyệt rồi gửi lên server -----
    function resize(file, maxSize, quality) {
        return new Promise(function (resolve, reject) {
            var img = new Image();
            var url = URL.createObjectURL(file);
            img.onload = function () {
                var scale = Math.min(1, maxSize / Math.max(img.width, img.height));
                var c = document.createElement('canvas');
                c.width = Math.round(img.width * scale);
                c.height = Math.round(img.height * scale);
                c.getContext('2d').drawImage(img, 0, 0, c.width, c.height);
                URL.revokeObjectURL(url);
                c.toBlob(function (blob) {
                    resolve({ blob: blob, dataUrl: c.toDataURL('image/jpeg', 0.6) });
                }, 'image/jpeg', quality);
            };
            img.onerror = function () { URL.revokeObjectURL(url); reject(); };
            img.src = url;
        });
    }

    function askImage(file) {
        if (!file || busy) return;
        var note = input.value.trim();
        input.value = '';
        busy = true;

        Promise.all([resize(file, 1280, 0.85), resize(file, 240, 0.6)])
            .then(function (r) {
                history.push({ role: 'user', content: '[Khách gửi ảnh phụ tùng] ' + note, text: note, img: r[1].dataUrl });
                render();
                var fd = new FormData();
                fd.append('image', r[0].blob, 'photo.jpg');
                fd.append('message', note);
                return fetch(imageUrl, { method: 'POST', headers: { 'RequestVerificationToken': token }, body: fd });
            })
            .then(function (res) { if (!res.ok) throw new Error(res.status); return res.json(); })
            .then(function (data) {
                history.push({ role: 'assistant', content: data.reply, products: data.products });
            })
            .catch(function () {
                history.push({ role: 'assistant', content: 'Xin lỗi, mình chưa gửi được ảnh này. Bạn thử ảnh khác hoặc gõ tên phụ tùng giúp mình nha.' });
            })
            .finally(function () {
                busy = false;
                fileInput.value = '';
                save(history);
                render();
            });
    }

    fileInput.addEventListener('change', function () {
        if (fileInput.files && fileInput.files[0]) askImage(fileInput.files[0]);
    });

    function toggle(open) {
        var show = typeof open === 'boolean' ? open : panel.hidden;
        panel.hidden = !show;
        root.classList.toggle('open', show);
        if (show) { render(); input.focus(); }
    }

    root.addEventListener('click', function (e) {
        if (e.target.closest('[data-chat-toggle]')) toggle();
        else if (e.target.closest('[data-chat-reset]')) { history = []; save(history); render(); }
        else if (e.target.closest('[data-chat-photo]')) fileInput.click();
        else if (e.target.closest('[data-chat-quick]')) ask(e.target.closest('[data-chat-quick]').textContent);
    });
    form.addEventListener('submit', function (e) {
        e.preventDefault();
        var text = input.value;
        input.value = '';
        ask(text);
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !panel.hidden) toggle(false);
    });
})();
