(function () {
    var STORAGE_KEY = 'nhatduc.rememberLogin';

    function getFields() {
        var username = document.getElementById('username');
        var password = document.getElementById('password');
        var remember = document.getElementById('rememberMe');
        if (!username || !password || !remember) {
            return null;
        }

        return { username: username, password: password, remember: remember };
    }

    function encodePassword(value) {
        return btoa(unescape(encodeURIComponent(value || '')));
    }

    function decodePassword(value) {
        return decodeURIComponent(escape(atob(value || '')));
    }

    function loadRemembered() {
        var fields = getFields();
        if (!fields) {
            return;
        }

        try {
            var raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) {
                return;
            }

            var data = JSON.parse(raw);
            if (!data || !data.username) {
                return;
            }

            fields.username.value = data.username;
            fields.password.value = data.password ? decodePassword(data.password) : '';
            fields.remember.checked = true;
        } catch (e) {
            localStorage.removeItem(STORAGE_KEY);
        }
    }

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || !form.classList || !form.classList.contains('login-form')) {
            return;
        }

        var fields = getFields();
        if (!fields) {
            return;
        }

        if (fields.remember.checked) {
            localStorage.setItem(STORAGE_KEY, JSON.stringify({
                username: fields.username.value,
                password: encodePassword(fields.password.value)
            }));
        } else {
            localStorage.removeItem(STORAGE_KEY);
        }
    });

    function scheduleLoad() {
        loadRemembered();
        setTimeout(loadRemembered, 0);
        setTimeout(loadRemembered, 100);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', scheduleLoad);
    } else {
        scheduleLoad();
    }

    document.addEventListener('enhancedload', scheduleLoad);
})();
