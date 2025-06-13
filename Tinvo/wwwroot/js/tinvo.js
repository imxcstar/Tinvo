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

        // 用唯一id区分每个代码块
        const codeBlockId = `code-block-${Math.random().toString(36).substr(2, 9)}`;

        const lines = `
            <div class="source-code-line-numbers">
                ${lineNumbers.join('\n')}
            </div>
            <div class="source-code-line-values">
                ${lineValues.join('\n')}
            </div>`;
        return `
            <div class="code-container" id="${codeBlockId}">
                <div style="display: flex;flex-direction: column;max-width: 100%">
                    <div class="code-container-toolbar">
                        <div class="code-toolbar-lang">${langName}</div>
                        <div class="code-toolbar-buttons">
                            <button class="markdown-it-code-copy" data-clipboard-text="${content}" onclick="copyCode()" title="复制">
                                <span style="${options.iconStyle}" class="${options.iconClass}"></span>
                            </button>
                            <button class="markdown-it-code-toggle" onclick="toggleCodeVisibility('${codeBlockId}')" title="收起/展开">
                                <span class="toggle-icon">▼</span>
                            </button>
                        </div>
                    </div>
                    <pre class="language-${langName} code-content"><div class="source-code">${lines}</div></pre>
                </div>
            </div>`;
    };
}

function toggleCodeVisibility(codeBlockId) {
    const container = document.getElementById(codeBlockId);
    if (!container) return;
    const codeContent = container.querySelector('.code-content');
    const toggleIcon = container.querySelector('.toggle-icon');
    if (!codeContent || !toggleIcon) return;

    if (codeContent.classList.contains('collapsed')) {
        codeContent.classList.remove('collapsed');
        toggleIcon.textContent = '▼';
    } else {
        codeContent.classList.add('collapsed');
        toggleIcon.textContent = '▲';
    }
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