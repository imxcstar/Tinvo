const appHeight = () => {
    const doc = document.documentElement
    doc.style.setProperty('--app-height', `${window.innerHeight}px`)
}
window.addEventListener('resize', appHeight)
window.addEventListener("DOMContentLoaded", appHeight);
appHeight()

new ClipboardJS('.markdown-it-code-copy');

const defaultOptions = {
    iconStyle: 'font-size: 21px; opacity: 0.4;',
    iconClass: 'mdi mdi-content-copy',
};

function copyCode() {
    window.sendSuccessMessage('复制成功');
}

function renderCode(md, origRule, options) {
    options = _.merge(defaultOptions, options);
    return (...args) => {
        const [tokens, idx] = args;
        const content = tokens[idx].content
            .replaceAll('"', '&quot;');
        const origRendered = origRule(...args);

        if (content.length === 0)
            return origRendered;

        const langName = tokens[idx].info.trim();
        const lineNumbers = [];
        const lineValues = [];
        getCodeContent(origRendered).split('\n').forEach((line, i, arr) => {
            if (!(i === arr.length - 1 && line.trim() === '')) {
                lineNumbers.push(`<div class="source-code-line source-code-line-number">${i + 1}</div>`);
                lineValues.push(`<div class="source-code-line source-code-line-value">${line}</div>`);
            }
        });
        const lines = `
            <div class="source-code-line-numbers">
                ${lineNumbers.join('\n')}
            </div>
            <div class="source-code-line-values">
                ${lineValues.join('\n')}
            </div>`;
        return `
            <div class="code-container">
                <div style="display: flex;flex-direction: column;max-width: 100%">
                    <div class="code-container-toolbar">
                        <p>${langName}</p>
                        <button class="markdown-it-code-copy" data-clipboard-text="${content}" onclick="copyCode()" title="复制">
                            <span style="${options.iconStyle}" class="${options.iconClass}"></span>
                        </button>
                    </div>
                    <pre class="language-${langName}"><div class="source-code">${lines}</div></pre>
                </div>
            </div>`;
    };
}

function getCodeContent(origRendered) {
    const parser = new DOMParser();
    const doc = parser.parseFromString(origRendered, 'text/html');
    const codeElement = doc.querySelector('code');
    return codeElement ? codeElement.innerHTML : '';
}

window.MasaBlazor.extendMarkdownIt = function (parser) {
    const {md} = parser;
    md.use((md, options) => {
        md.renderer.rules.code_block = renderCode(md, md.renderer.rules.code_block, options);
        md.renderer.rules.fence = renderCode(md, md.renderer.rules.fence, options);
    });
}

let timer = null

window.ChatContainerToBottom = (force = true, animation = true) => {
    var div = document.getElementById('ChatContainer');
    var threshold = 300;
    if (force) {
        if (animation)
            div.scrollTo({top: 9999999, behavior: 'smooth'});
        else
            div.scrollTo({top: 9999999});
    } else {
        if (div.scrollHeight - div.scrollTop - div.clientHeight < threshold) {
            if (animation)
                div.scrollTo({top: 9999999, behavior: 'smooth'});
            else
                div.scrollTo({top: 9999999});
        }
    }
}

window.StartChatContainerCheck = () => {
    if (timer != null)
        clearInterval(timer);
    var div = document.getElementById('ChatContainer');
    var threshold = 300;
    timer = setInterval(function () {
        try {
            if (div.scrollHeight - div.scrollTop - div.clientHeight < threshold) {
                div.scrollTo({top: 9999999, behavior: 'smooth'});
            }
        } catch (e) {
        }
    }, 800);
}

window.StopChatContainerCheck = () => {
    if (timer != null)
        clearInterval(timer);
}