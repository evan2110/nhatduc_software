window.announcementEditor = {
    _loading: null,
    _maxImageSide: 1600,
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
    create: function (element, options, dotnetRef) {
        var self = this;
        options = options || {};
        return this.ensure().then(function () {
            if (!element || element._suneditor) {
                return;
            }

            var upload = !!(options.imageUpload && dotnetRef);
            var editor = SUNEDITOR.create(element, {
                width: "100%",
                height: "auto",
                minHeight: options.minHeight || "120px",
                maxHeight: options.maxHeight || "260px",
                showPathLabel: false,
                charCounter: true,
                maxCharCount: options.maxCharCount || 5000,
                placeholder: options.placeholder || "Nhập nội dung thông báo",
                imageFileInput: upload,
                imageAccept: ".jpg,.jpeg,.png,.webp,.gif",
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

            if (upload) {
                editor.onImageUploadBefore = function (files, info, core, uploadHandler) {
                    self._uploadImages(files, dotnetRef).then(function (result) {
                        uploadHandler({ result: result });
                    }).catch(function (error) {
                        core.closeLoading();
                        window.alert(error && error.message ? error.message : "Không tải được ảnh.");
                    });
                    return undefined;
                };
            }

            self._patchVideoUrl(editor);
            if (options.html) {
                editor.setContents(options.html);
            }

            element._suneditor = editor;
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
    },
    setHtml: function (element, html) {
        if (!element || !element._suneditor) {
            return;
        }

        element._suneditor.setContents(html || "");
    },
    // Turns Google Drive and YouTube Shorts links into players that can be embedded.
    normalizeVideoUrl: function (url) {
        var value = (url || "").trim();
        var drive = /^(?:https?:\/\/)?drive\.google\.com\/(?:file\/d\/|open\?id=)([\w-]{10,})/i.exec(value);
        if (drive) {
            // SunEditor makes an <iframe> only when the URL says "embed"; Drive ignores the extra parameter.
            return "https://drive.google.com/file/d/" + drive[1] + "/preview?embedded=true";
        }

        var shorts = /^(?:https?:\/\/)?(?:www\.|m\.)?youtube\.com\/shorts\/([\w-]{6,})/i.exec(value);
        if (shorts) {
            return "https://www.youtube.com/embed/" + shorts[1];
        }

        return url;
    },
    _patchVideoUrl: function (editor) {
        var self = this;
        var plugins = editor.core && editor.core.plugins;
        var video = plugins && plugins.video;
        if (!video || video._nhatDucPatched || typeof video.setup_url !== "function") {
            return;
        }

        var original = video.setup_url;
        video.setup_url = function (url) {
            var value = url || (this.context && this.context.video && this.context.video._linkValue) || "";
            return original.call(this, self.normalizeVideoUrl(value));
        };
        video._nhatDucPatched = true;
    },
    _uploadImages: async function (files, dotnetRef) {
        var result = [];
        for (var i = 0; i < files.length; i++) {
            var original = files[i];
            var blob = await this._shrinkImage(original);
            var bytes = new Uint8Array(await blob.arrayBuffer());
            var response = await dotnetRef.invokeMethodAsync("UploadImage", bytes.length, DotNet.createJSStreamReference(bytes));
            if (!response || !response.url) {
                throw new Error((response && response.error) || "Không tải được ảnh.");
            }

            result.push({ url: response.url, name: original.name, size: bytes.length });
        }

        return result;
    },
    // Large photos from a phone are scaled down before upload so the page stays light.
    _shrinkImage: async function (file) {
        if (!file || /gif$/i.test(file.type) || typeof createImageBitmap !== "function") {
            return file;
        }

        try {
            var bitmap = await createImageBitmap(file);
            var side = Math.max(bitmap.width, bitmap.height);
            if (side <= this._maxImageSide && file.size <= 1024 * 1024) {
                bitmap.close && bitmap.close();
                return file;
            }

            var scale = Math.min(1, this._maxImageSide / side);
            var canvas = document.createElement("canvas");
            canvas.width = Math.max(1, Math.round(bitmap.width * scale));
            canvas.height = Math.max(1, Math.round(bitmap.height * scale));
            canvas.getContext("2d").drawImage(bitmap, 0, 0, canvas.width, canvas.height);
            bitmap.close && bitmap.close();

            var blob = await new Promise(function (resolve) { canvas.toBlob(resolve, "image/webp", 0.85); });
            if (blob && blob.type === "image/webp" && blob.size < file.size) {
                return blob;
            }
        } catch (e) {
        }

        return file;
    }
};
