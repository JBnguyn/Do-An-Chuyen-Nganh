// ===== 3H AUTO - trang quản trị =====

// ----- Cảnh báo "Có thay đổi chưa lưu" cho các form thêm / sửa -----
// Tự áp dụng cho mọi form POST trong <main> có ô nhập. Thêm data-no-unsaved vào form để bỏ qua.
(function () {
    var tracked = [];
    var leaving = false; // đang gửi form hoặc bấm nút Hủy => không hỏi lại

    /** Chụp giá trị hiện tại của form thành một chuỗi để so sánh. */
    function snapshot(form) {
        return Array.prototype.map.call(form.elements, function (el) {
            if (!el.name || el.disabled || el.name === '__RequestVerificationToken') return '';
            if (el.type === 'submit' || el.type === 'button') return '';
            if (el.type === 'checkbox' || el.type === 'radio') return el.name + '=' + el.checked;
            if (el.type === 'file') return el.name + '=' + el.files.length;
            return el.name + '=' + el.value;
        }).join('\n');
    }

    function update(item) {
        item.dirty = item.invalid || snapshot(item.form) !== item.initial;
        if (item.hint) item.hint.hidden = !item.dirty;
    }

    Array.prototype.forEach.call(document.querySelectorAll('main form:not([data-no-unsaved])'), function (form) {
        if ((form.getAttribute('method') || '').toLowerCase() !== 'post') return;
        if (!form.querySelector('input:not([type=hidden]):not([type=submit]), select, textarea')) return;

        var item = {
            form: form,
            initial: snapshot(form),
            // Form vừa bị trả về vì lỗi nhập liệu: dữ liệu trên màn hình vẫn chưa được lưu
            invalid: !!form.querySelector('.input-validation-error, .validation-summary-errors'),
            dirty: false,
            hint: null
        };

        var submit = form.querySelector('button[type=submit], input[type=submit]');
        if (submit) {
            item.hint = document.createElement('span');
            item.hint.className = 'unsaved-hint';
            item.hint.hidden = true;
            item.hint.innerHTML = '<i class="bi bi-exclamation-circle"></i> Có thay đổi chưa lưu';
            submit.parentNode.appendChild(item.hint);
        }

        form.addEventListener('input', function () { update(item); });
        form.addEventListener('change', function () { update(item); });
        // Nút "Hủy" / "Quay lại" trong form: người dùng chủ động bỏ thay đổi
        form.addEventListener('click', function (e) {
            if (e.target.closest('a.btn')) leaving = true;
        });

        update(item);
        tracked.push(item);
    });

    if (!tracked.length) return;

    // Chạy sau các trình xử lý trên form (vd. jQuery Validation) nên biết form có thật sự được gửi không
    document.addEventListener('submit', function (e) {
        if (!e.defaultPrevented) leaving = true;
    });

    window.addEventListener('beforeunload', function (e) {
        if (leaving) return;
        if (!tracked.some(function (item) { return item.dirty; })) return;
        e.preventDefault();
        e.returnValue = '';
    });
})();
