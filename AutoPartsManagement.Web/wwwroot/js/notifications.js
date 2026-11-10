// ===== Chuông thông báo cho Admin =====
// Hỏi server mỗi 15 giây: có thông báo mới (đơn hàng mới, hủy đơn, sắp hết hàng) thì
// cập nhật số trên chuông và hiện thông báo nhỏ ở góc màn hình.
(function () {
    'use strict';

    var root = document.querySelector('[data-notif]');
    if (!root) return;

    var feedUrl = root.getAttribute('data-feed-url');
    var readAllUrl = root.getAttribute('data-readall-url');
    var countEl = root.querySelector('[data-notif-count]');
    var listEl = root.querySelector('[data-notif-list]');
    var readAllBtn = root.querySelector('[data-notif-readall]');
    var toastBox = document.querySelector('[data-notif-toasts]');
    var tokenEl = root.querySelector('input[name="__RequestVerificationToken"]');
    var baseTitle = document.title;
    var lastId = null;          // id thông báo mới nhất đã biết (null = lần tải đầu)
    var POLL_MS = 15000;

    function esc(s) {
        var d = document.createElement('div');
        d.textContent = s == null ? '' : String(s);
        return d.innerHTML;
    }

    function itemHtml(n) {
        return '<a class="notif-item' + (n.isRead ? '' : ' is-unread') + '" href="' + esc(n.url) + '">' +
            '<span class="notif-icon ' + esc(n.css) + '"><i class="bi ' + esc(n.icon) + '"></i></span>' +
            '<span class="notif-body">' +
            '<span class="notif-title">' + esc(n.title) + '</span>' +
            '<span class="notif-msg">' + esc(n.message) + '</span>' +
            '<span class="notif-time">' + esc(n.time) + '</span>' +
            '</span>' +
            (n.isRead ? '' : '<span class="notif-dot"></span>') +
            '</a>';
    }

    function render(data) {
        var unread = data.unread || 0;
        countEl.textContent = unread > 99 ? '99+' : unread;
        countEl.hidden = unread === 0;
        readAllBtn.hidden = unread === 0;
        document.title = unread > 0 ? '(' + unread + ') ' + baseTitle : baseTitle;

        listEl.innerHTML = data.items && data.items.length
            ? data.items.map(itemHtml).join('')
            : '<div class="notif-empty"><i class="bi bi-bell-slash"></i> Chưa có thông báo nào</div>';
    }

    function showToasts(items) {
        if (!toastBox) return;
        items.slice(0, 3).reverse().forEach(function (n) {
            var el = document.createElement('a');
            el.className = 'notif-toast';
            el.href = n.url;
            el.innerHTML = '<span class="notif-icon ' + esc(n.css) + '"><i class="bi ' + esc(n.icon) + '"></i></span>' +
                '<span class="notif-body"><span class="notif-title">' + esc(n.title) + '</span>' +
                '<span class="notif-msg">' + esc(n.message) + '</span></span>';
            toastBox.appendChild(el);
            requestAnimationFrame(function () { el.classList.add('show'); });
            setTimeout(function () {
                el.classList.remove('show');
                setTimeout(function () { el.remove(); }, 400);
            }, 8000);
        });
        ding();
        countEl.classList.remove('bump');
        void countEl.offsetWidth;          // chạy lại hiệu ứng rung chuông
        countEl.classList.add('bump');
    }

    // Tiếng "ting" nhẹ (trình duyệt có thể chặn nếu chưa bấm gì trên trang)
    function ding() {
        try {
            var Ctx = window.AudioContext || window.webkitAudioContext;
            if (!Ctx) return;
            var ctx = new Ctx();
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(880, ctx.currentTime);
            osc.frequency.setValueAtTime(1320, ctx.currentTime + 0.12);
            gain.gain.setValueAtTime(0.0001, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.15, ctx.currentTime + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.4);
            osc.connect(gain).connect(ctx.destination);
            osc.start();
            osc.stop(ctx.currentTime + 0.42);
            osc.onended = function () { ctx.close(); };
        } catch (e) { /* bỏ qua */ }
    }

    function load() {
        return fetch(feedUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' }, credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (data) {
                if (!data) return;
                if (lastId !== null && data.latestId > lastId) {
                    showToasts(data.items.filter(function (n) { return n.id > lastId && !n.isRead; }));
                }
                lastId = data.latestId;
                render(data);
            })
            .catch(function () { /* mất mạng tạm thời: lần sau thử lại */ });
    }

    readAllBtn.addEventListener('click', function () {
        fetch(readAllUrl, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': tokenEl ? tokenEl.value : ''
            }
        }).then(load);
    });

    load();
    setInterval(function () {
        if (!document.hidden) load();
    }, POLL_MS);
    document.addEventListener('visibilitychange', function () {
        if (!document.hidden) load();
    });
})();
