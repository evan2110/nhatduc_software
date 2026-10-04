window.announcementEditor = {
    exec: function (element, command, value) {
        if (!element) return;
        element.focus();
        document.execCommand(command, false, value || null);
    },
    link: function (element) {
        if (!element) return;
        var url = window.prompt("Nhập địa chỉ liên kết (https://...)");
        if (!url) return;
        element.focus();
        document.execCommand("createLink", false, url.trim());
    },
    getHtml: function (element) {
        return element ? element.innerHTML : "";
    }
};
