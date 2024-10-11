function loadScript(url, callback) {
    var script = document.createElement("script")
    script.type = "text/javascript";
    if (script.readyState) {
        script.onreadystatechange = function () {
            if (script.readyState == "loaded" ||
                script.readyState == "complete") {
                script.onreadystatechange = null;
                callback();
            }
        };
    } else {
        script.onload = function () {
            callback();
        };
    }
    script.src = url;
    document.getElementsByTagName("head")[0].appendChild(script);
}

window.blazorHelper = {
    checkScrollEnd: function (id) {
        var d = document.getElementById(id);
        return (d.offsetHeight + d.scrollTop >= d.scrollHeight);
    },
    InitScrollEndListener: function (className, dotNetReference, methodName) {
        var maybeInvokeMethod = function (element) {
            var checkScrollAtBottom = function () {
                return (element.scrollHeight - element.scrollTop) === element.clientHeight;
            };
            if (checkScrollAtBottom()) {
                dotNetReference.invokeMethodAsync(methodName);
            }
        };
        var lastTouchY = 0;
        var onWheel = function (event) {
            var element = event.target.closest('.' + className);
            if (element != null) {
                var delta = event.deltaY;
                if (delta > 0 && maybeInvokeMethod(element)) {
                    event.preventDefault();
                }
            }
        };
        var onTouchStart = function (event) {
            if (event.touches.length === 1) {
                lastTouchY = event.touches[0].clientY;
            }
        };
        var onTouchMove = function (event) {
            if (event.touches.length === 1) {
                var touchY = event.touches[0].clientY;
                var delta = touchY - lastTouchY;
                var element = event.target.closest('.' + className);
                if (element != null) {
                    if (delta < 0 && maybeInvokeMethod(element)) {
                        event.preventDefault();
                    }
                    lastTouchY = touchY;
                }
            }
        };
        document.addEventListener('wheel', onWheel, { passive: false });
        document.addEventListener('touchstart', onTouchStart, { passive: true });
        document.addEventListener('touchmove', onTouchMove, { passive: false });
    },
    GetElementValueById: function (id) {
        return document.getElementById(id).value;
    },
    SetElementValueById: function (id, value) {
        document.getElementById(id).value = value;
    },
    SetElementValueByClass: function (className, value) {
        var elements = document.getElementsByClassName(className);
        for (var i = 0; i < elements.length; i++) {
            elements[i].value = value;
        }
    },
    OnKeyDown: function (className, dotNetReference, methodName) {
        var elements = document.getElementsByClassName(className);

        for (var i = 0; i < elements.length; i++) {
            elements[i].addEventListener('keydown', async function (e) {
                var eventArgs = {
                    key: e.key || "Unidentified",
                    code: e.code || "Unidentified",
                    location: e.location || 0,
                    repeat: e.repeat || false,
                    ctrlKey: e.ctrlKey || false,
                    shiftKey: e.shiftKey || false,
                    altKey: e.altKey || false,
                    metaKey: e.metaKey || false,
                    type: e.type || "keydown"
                };
                var result = await dotNetReference.invokeMethodAsync(methodName, eventArgs, e.target.value || "");
                if (result === false) {
                    e.preventDefault();
                    e.stopPropagation();
                }
            });
        }
    },
    OnKeyDownListen: function (className, listenKeyInfo, dotNetReference, beforeMethodName, afterMethodName) {
        function checkKeyCombination(e, keys) {
            let keyArr = keys.split('+').map(item => item.trim().toLowerCase());
            let ctrlKey = keyArr.includes('ctrl') ? e.ctrlKey : keyArr.includes('!ctrl') ? !e.ctrlKey : true;
            let shiftKey = keyArr.includes('shift') ? e.shiftKey : keyArr.includes('!shift') ? !e.shiftKey : true;
            let altKey = keyArr.includes('alt') ? e.altKey : keyArr.includes('!alt') ? !e.altKey : true;
            keyArr = keyArr.filter(k => !(/^(ctrl|!ctrl|shift|!shift|alt|!alt)$/.test(k)));
            let keyCode = keyArr.length ? e.key.toLowerCase() === keyArr[0] : true;
            return ctrlKey && shiftKey && altKey && keyCode;
        }

        var elements = document.getElementsByClassName(className);

        for (var i = 0; i < elements.length; i++) {
            elements[i].addEventListener('keydown', async function (e) {
                if (checkKeyCombination(e, listenKeyInfo)) {
                    var value = e.target.value;
                    var result = await dotNetReference.invokeMethodAsync(beforeMethodName, value);
                    if (result === true) {
                        e.preventDefault();
                        e.stopPropagation();
                        e.target.value = '';
                    }
                    await dotNetReference.invokeMethodAsync(afterMethodName, value);
                }
            })
        }
    },
    DownloadFileFromStream: async function (fileName, contentStreamReference) {
        const arrayBuffer = await contentStreamReference.arrayBuffer();
        const blob = new Blob([arrayBuffer]);
        const url = URL.createObjectURL(blob);
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = fileName ?? '';
        anchorElement.click();
        anchorElement.remove();
        URL.revokeObjectURL(url);
    }
}

window.loopWithInterval = (dotNetHelper, methodName, interval) => {
    var intervalId = setInterval(() => {
        dotNetHelper.invokeMethodAsync(methodName)
            .then(isContinue => {
                if (!isContinue) {
                    clearInterval(intervalId);
                }
            });
    }, interval);

    return {
        dispose: () => {
            clearInterval(intervalId);
        }
    };
};