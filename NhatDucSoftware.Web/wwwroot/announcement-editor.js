window.announcementEditor = {
    _loading: null,
    ensure: function () {
        if (window.SUNEDITOR) {
            return Promise.resolve();
        }

        if (this._loading) {
            return this._loading;
        }

        this._loading = new Promise(function (resolve, reject) {
            var script = document.createElement("script");
            script.src = "/lib/suneditor/suneditor.min.js";
            script.onload = function () { resolve(); };
            script.onerror = function () { reject(new Error("Không tải được trình soạn thảo.")); };
            document.body.appendChild(script);
        });

        return this._loading;
    },
    create: function (element) {
        return this.ensure().then(function () {
            if (!element || element._suneditor) {
                return;
            }

            element._suneditor = SUNEDITOR.create(element, {
                width: "100%",
                height: "auto",
                minHeight: "120px",
                maxHeight: "260px",
                showPathLabel: false,
                charCounter: true,
                maxCharCount: 5000,
                placeholder: "Nhập nội dung thông báo",
                imageFileInput: false,
                videoFileInput: false,
                buttonList: [
                    ["undo", "redo", "font", "fontSize", "formatBlock"],
                    ["bold", "underline", "italic", "strike", "subscript", "superscript", "removeFormat"],
                    "/",
                    ["fontColor", "hiliteColor", "outdent", "indent", "align", "horizontalRule", "list", "table"],
                    ["link", "image", "video", "fullScreen", "showBlocks", "codeView", "preview", "print", "save"]
                ],
                callBackSave: function () { }
            });
        });
    },
    destroy: function (element) {
        if (!element || !element._suneditor) {
            return;
        }

        element._suneditor.destroy();
        element._suneditor = null;
    },
    getHtml: function (element) {
        if (!element || !element._suneditor) {
            return "";
        }

        return element._suneditor.getContents(true);
    }
};
