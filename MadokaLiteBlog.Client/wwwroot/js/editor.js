window.getCaretPosition = function (elementId) {
    var element = document.getElementById(elementId);
    if (!element) return 0;
    var caretPos = 0;
    if (element.selectionStart || element.selectionStart === 0) {
        caretPos = element.selectionStart;
    }
    return caretPos;
};
window.handlePasteEvent = function (elementId) {
    var element = document.getElementById(elementId);
    if (!element) return;

    console.log("添加事件监听");
    element.addEventListener('paste', function (event) {
        // 阻止默认的粘贴行为
        event.preventDefault();

        var clipboardData = event.clipboardData || window.clipboardData;

        // 检查 clipboardData 是否有效
        if (!clipboardData) {
            console.error("Clipboard data is not available.");
            return;
        }
        var text = clipboardData.getData('Text');
        if (clipboardData.items && clipboardData.items.length > 0) {
            var items = clipboardData.items;
            for (var i = 0; i < items.length; i++) {
                var item = items[i];
                if (item.type.indexOf("image") === 0) {
                    var file = item.getAsFile();
                    var reader = new FileReader();

                    reader.onloadend = function () {
                        var base64Image = reader.result.split(',')[1];
                        // 调用 Blazor 方法处理 Base64 图片
                        window.DotNet.invokeMethodAsync('MadokaLiteBlog.Client', 'HandleImagePaste', base64Image)
                            .then(function (modifiedData) {
                                insertAtCaret(elementId, modifiedData);
                            });
                    };
                    reader.readAsDataURL(file);
                    return;
                }
            }
        }

        insertAtCaret(elementId, text);
    });
};
window.insertAtCaret = function (elementId, textToInsert) {
    var element = document.getElementById(elementId);
    if (!element) return;

    var currentValue = element.value;
    var caretPos = element.selectionStart;

    element.value = currentValue.substring(0, caretPos) + textToInsert + currentValue.substring(caretPos);
    element.selectionStart = element.selectionEnd = caretPos + textToInsert.length;
};