export * from "prosemirror-state";
export * from "prosemirror-view";
export * from "prosemirror-schema-basic";
export * from "prosemirror-history";
export * from "prosemirror-schema-list";
export * from "prosemirror-keymap";
export * from "prosemirror-commands";
export * from "prosemirror-markdown";
export * from "@benrbray/prosemirror-math";
export * from "prosemirror-inputrules";
export * from "prosemirror-model";
export * from "markdown-it-texmath";
export * from "markdown-it";

import {EditorState} from "prosemirror-state"
import {EditorView} from "prosemirror-view"
import { baseKeymap } from "prosemirror-commands";
import {Schema} from "prosemirror-model"
import { schema as basicSchema } from "prosemirror-schema-basic";
import {keymap} from "prosemirror-keymap"
import { chainCommands, deleteSelection, joinBackward, selectNodeBackward} from "prosemirror-commands"
import { MarkdownParser } from "prosemirror-markdown";
import { history, undo, redo } from "prosemirror-history";
import {defaultMarkdownSerializer} from "prosemirror-markdown"
import texmath from 'markdown-it-texmath';
import MarkdownIt from 'markdown-it';
import {
    inputRules,
    textblockTypeInputRule,
    wrappingInputRule,
    InputRule,
} from "prosemirror-inputrules"
import {
    mathPlugin, 
    mathBackspaceCmd, 
    makeInlineMathInputRule,
    makeBlockMathInputRule,
    REGEX_INLINE_MATH_DOLLARS,
    REGEX_BLOCK_MATH_DOLLARS,
    insertMathCmd,
} from "@benrbray/prosemirror-math"
import { MarkdownSerializer } from "prosemirror-markdown";

function listIsTight(tokens, i) {
    while (++i < tokens.length)
      if (tokens[i].type != "list_item_open") return tokens[i].hidden
    return false
}

const mathNodes = {
    doc: {
        content: "block+"
    },
    paragraph: {
        content: "inline*",
        group: "block",
        parseDOM: [{tag: "p"}],
        toDOM() { return ["p", 0] }
    },
    blockquote: {
        content: "block+",
        group: "block",
        parseDOM: [{tag: "blockquote"}],
        toDOM() { return ["blockquote", 0] }
    },
    horizontal_rule: {
        group: "block",
        parseDOM: [{tag: "hr"}],
        toDOM() { return ["div", ["hr"]] }
    },
    heading: {
        attrs: {level: {default: 1}},
        content: "(text | image)*",
        group: "block",
        defining: true,
        parseDOM: [{tag: "h1", attrs: {level: 1}},
                    {tag: "h2", attrs: {level: 2}},
                    {tag: "h3", attrs: {level: 3}},
                    {tag: "h4", attrs: {level: 4}},
                    {tag: "h5", attrs: {level: 5}},
                    {tag: "h6", attrs: {level: 6}}],
        toDOM(node) { return ["h" + node.attrs.level, 0] }
    },
    code_block: {
        content: "text*",
        group: "block",
        code: true,
        defining: true,
        marks: "",
        attrs: {params: {default: ""}},
        parseDOM: [{tag: "pre", preserveWhitespace: "full", getAttrs: node => (
            {params: (node).getAttribute("data-params") || ""}
        )}],
        toDOM(node) { return ["pre", node.attrs.params ? {"data-params": node.attrs.params} : {}, ["code", 0]] }
    },
    ordered_list: {
        content: "list_item+",
        group: "block",
        attrs: {order: {default: 1}, tight: {default: false}},
        parseDOM: [{tag: "ol", getAttrs(dom) {
            return {order: (dom).hasAttribute("start") ? +(dom).getAttribute("start") : 1,
                    tight: (dom).hasAttribute("data-tight")}
        }}],
        toDOM(node) {
            return ["ol", {start: node.attrs.order == 1 ? null : node.attrs.order,
                            "data-tight": node.attrs.tight ? "true" : null}, 0]
        }
    },
    bullet_list: {
        content: "list_item+",
        group: "block",
        attrs: {tight: {default: false}},
        parseDOM: [{tag: "ul", getAttrs: dom => ({tight: (dom).hasAttribute("data-tight")})}],
        toDOM(node) { return ["ul", {"data-tight": node.attrs.tight ? "true" : null}, 0] }
    },
    list_item: {
        content: "block+",
        defining: true,
        parseDOM: [{tag: "li"}],
        toDOM() { return ["li", 0] }
    },
    text: {
        group: "inline"
    },
    image: {
        inline: true,
        attrs: {
            src: {},
            alt: {default: null},
            title: {default: null}
        },
        group: "inline",
        draggable: true,
        parseDOM: [{tag: "img[src]", getAttrs(dom) {
            return {
            src: (dom).getAttribute("src"),
            title: (dom).getAttribute("title"),
            alt: (dom).getAttribute("alt")
            }
        }}],
        toDOM(node) { return ["img", node.attrs] }
    },
    hard_break: {
        inline: true,
        group: "inline",
        selectable: false,
        parseDOM: [{tag: "br"}],
        toDOM() { return ["br"] }
    },
    math_inline: {
        group: "inline math", 
        content: "text*",
        inline: true,
        atom: true,
        toDOM: () => ["math-inline", { class: "math-node" }, 0],
        parseDOM: [{
            tag: "math-inline"
        }]
    },
    math_display: {
        group: "block math",
        content: "text*",
        atom: true,
        code: true,
        toDOM: () => ["math-display", { class: "math-node" }, 0],
        parseDOM: [{
            tag: "math-display"
        }]
    },
};

const latexSchema = new Schema({
    nodes: mathNodes,
    marks: {
        ...basicSchema.spec.marks,
        strong: {
            parseDOM: [
                {tag: "strong"},
                {tag: "b"},
                {style: "font-weight", getAttrs: value => value === "bold" || value === "700"}
            ],
            toDOM() { return ["strong"] },
            inclusive: false
        },
        em: {
            parseDOM: [
                {tag: "i"},
                {tag: "em"},
                {style: "font-style=italic"}
            ],
            toDOM() { return ["em"] },
            inclusive: false
        },
        link: {
            attrs: {
              href: {},
              title: {default: null}
            },
            inclusive: false,
            parseDOM: [{tag: "a[href]", getAttrs(dom) {
              return {href: (dom).getAttribute("href"), title: (dom).getAttribute("title")}
            }}],
            toDOM(node) { return ["a", node.attrs] }
        },
        code: {
            parseDOM: [{tag: "code"}],
            toDOM() { return ["code"] },
            inclusive: false
        }
    },
});

const markdownItWithMath = (() => {
    const md = MarkdownIt('commonmark', { html: false });
  
    // 使用 texmath 插件，使用默认配置
    md.use(texmath);
    
    console.log("markdownItWithMath configured");
    
    return md;
})();

const latexParser = new MarkdownParser(
    latexSchema,
    markdownItWithMath,
    {
        blockquote: {block: "blockquote"},
        paragraph: {block: "paragraph"},
        list_item: {block: "list_item"},
        bullet_list: {block: "bullet_list", getAttrs: (_, tokens, i) => ({tight: listIsTight(tokens, i)})},
        ordered_list: {block: "ordered_list", getAttrs: (tok, tokens, i) => ({
          order: +tok.attrGet("start") || 1,
          tight: listIsTight(tokens, i)
        })},
        heading: {block: "heading", getAttrs: tok => ({level: +tok.tag.slice(1)})},
        code_block: {block: "code_block", getAttrs: tok => ({params: tok.info || ""}), noCloseToken: true},
        fence: {block: "code_block", getAttrs: tok => ({params: tok.info || ""}), noCloseToken: true},
        hr: {node: "horizontal_rule"},
        image: {node: "image", getAttrs: tok => ({
          src: tok.attrGet("src"),
          title: tok.attrGet("title") || null,
          alt: tok.children[0] && tok.children[0].content || null
        })},
        hardbreak: {node: "hard_break"},
      
        em: {mark: "em"},
        strong: {mark: "strong"},
        link: {mark: "link", getAttrs: tok => ({
          href: tok.attrGet("href"),
          title: tok.attrGet("title") || null
        })},
        code_inline: {mark: "code", noCloseToken: true},
        math_inline: { 
            node: 'math_inline', 
            noCloseToken: true,
            getAttrs: (tok) => {
                console.log('math_inline token:', tok);
                console.log('math_inline content:', tok.content);
                return {};
            }
        },
        math_block: {
            node: 'math_display',
            noCloseToken: true,
            getAttrs: (tok) => {
                console.log('math_block token:', tok);
                console.log('math_block content:', tok.content);
                return {};
            },
            getContent: (tok, schema) => {
                console.log('math_block getContent called with:', tok.content);
                // 创建包含数学内容的文本节点
                return schema.text(tok.content || "");
            }
        },
    }
);

const inlineMathInputRule = makeInlineMathInputRule(
    REGEX_INLINE_MATH_DOLLARS, 
    latexSchema.nodes.math_inline
);

const blockMathInputRule = makeBlockMathInputRule(
    REGEX_BLOCK_MATH_DOLLARS, 
    latexSchema.nodes.math_display
);

const mathPlugins = [
    mathPlugin,
    keymap({
        "Mod-Space": insertMathCmd(latexSchema.nodes.math_inline),
        "Backspace": chainCommands(deleteSelection, mathBackspaceCmd, joinBackward, selectNodeBackward),
    }),
    inputRules({ rules: [inlineMathInputRule, blockMathInputRule] })
];

function markInputRule(regexp, markType) {
    return new InputRule(regexp, (state, match, start, end) => {
        const fullMatch = match[0];
        const content = match[2] || match[1];
        
        if (markType === latexSchema.marks.strong) {
            if (fullMatch.startsWith('**') && !fullMatch.endsWith('**')) {
                return null;
            }
        }
        
        if (markType === latexSchema.marks.em) {
            const before = state.doc.textBetween(Math.max(0, start - 1), start);
            if (before === '*' && fullMatch.startsWith('*')) {
                return null;
            }
        }

        const tr = state.tr;
        tr.delete(start, end);
        tr.insertText(content, start);
        tr.addMark(start, start + content.length, markType.create());
        return tr;
    });
}

function markdownInputRules(schema) {
    return [
        textblockTypeInputRule(/^(#{1,6})\s$/, schema.nodes.heading, match => ({
            level: match[1].length
        })),
        markInputRule(
            /(\*\*)([^*\n]+)(\*\*)$/,
            schema.marks.strong
        ),
        markInputRule(
            /(?:^|[^*])(\*)([^*\n]+)(\*)$/,
            schema.marks.em
        ),
        markInputRule(
            /(`)([^`\n]+)(`)$/,
            schema.marks.code
        ),
        wrappingInputRule(/^\s*>\s$/, schema.nodes.blockquote),
        wrappingInputRule(
            /^(\d+)\.\s$/,
            schema.nodes.ordered_list,
            match => ({order: +match[1]}),
            (match, node) => node.childCount + node.attrs.order === +match[1]
        ),
        wrappingInputRule(/^\s*([-+*])\s$/, schema.nodes.bullet_list),
        textblockTypeInputRule(
            /^```(\w+)?$/,
            schema.nodes.code_block,
            match => ({ params: match[1] || "" })
        )
    ];
} 

// 存储编辑器实例的 Map
const editorInstances = new Map();

// 移除了 cleanMathContent 函数，因为它造成了更多问题
// 让 ProseMirror 和 markdown-it 自然地处理转义

function initializeEditor(elementId, markdown_content) {
    const editorElement = document.getElementById(elementId);
    console.log("initializeEditor called with elementId:", elementId);
    console.log("markdown_content:", markdown_content);
    console.log("editorElement found:", !!editorElement);
    
    if (!editorElement) {
        console.error("Editor element not found:", elementId);
        return null;
    }
    
    try {
        // 新的混合解决方案：先解析 Markdown，然后替换数学公式
        
        // 步骤1：存储数学公式并用占位符替换
        const mathBlocks = new Map();
        const mathInlines = new Map();
        let blockCounter = 0;
        let inlineCounter = 0;
        
        let processedContent = markdown_content;
        
        // 处理块公式 $$...$$
        processedContent = processedContent.replace(/\$\$\s*\n?([\s\S]*?)\n?\s*\$\$/g, (match, content) => {
            const placeholder = `MATHBLOCK${blockCounter}PLACEHOLDER`;
            let cleanedContent = content.trim();
            
            // 在加载时不需要清理，只是简单处理
            // cleanedContent = cleanMathContent(cleanedContent);
            console.log("Found block math:", cleanedContent);
            
            mathBlocks.set(placeholder, cleanedContent);
            blockCounter++;
            return placeholder;
        });
        
        // 处理行内公式 $...$
        processedContent = processedContent.replace(/\$([^$\n]+)\$/g, (match, content) => {
            const placeholder = `MATHINLINE${inlineCounter}PLACEHOLDER`;
            let cleanedContent = content.trim();
            
            // 在加载时不需要清理，只是简单处理
            // cleanedContent = cleanMathContent(cleanedContent);
            console.log("Found inline math:", cleanedContent);
            
            mathInlines.set(placeholder, cleanedContent);
            inlineCounter++;
            return placeholder;
        });
        
        console.log("Processed content with placeholders:", processedContent);
        
        // 步骤2：用普通的 markdown 解析器解析（不包含数学公式）
        const parsedDoc = latexParser.parse(processedContent);
        console.log("Parsed doc:", parsedDoc.toString());
        
        // 步骤3：遍历文档，替换占位符为数学节点
        function replacePlaceholders(node) {
            if (node.type.name === 'text') {
                const text = node.text;
                let hasChanges = false;
                let newContent = [];
                let currentIndex = 0;
                
                // 查找所有占位符
                const allPlaceholders = [];
                
                // 添加块公式占位符
                for (const [placeholder, mathContent] of mathBlocks.entries()) {
                    const index = text.indexOf(placeholder);
                    if (index !== -1) {
                        allPlaceholders.push({
                            index: index,
                            placeholder: placeholder,
                            content: mathContent,
                            type: 'block'
                        });
                    }
                }
                
                // 添加行内公式占位符
                for (const [placeholder, mathContent] of mathInlines.entries()) {
                    const index = text.indexOf(placeholder);
                    if (index !== -1) {
                        allPlaceholders.push({
                            index: index,
                            placeholder: placeholder,
                            content: mathContent,
                            type: 'inline'
                        });
                    }
                }
                
                // 按位置排序
                allPlaceholders.sort((a, b) => a.index - b.index);
                
                // 替换占位符
                for (const item of allPlaceholders) {
                    // 添加占位符前的文本
                    if (item.index > currentIndex) {
                        const textBefore = text.substring(currentIndex, item.index);
                        if (textBefore) {
                            newContent.push(latexSchema.text(textBefore));
                        }
                    }
                    
                    // 创建数学节点
                    console.log(`Replacing ${item.type} math placeholder with:`, item.content);
                    const mathNode = item.type === 'block' 
                        ? latexSchema.nodes.math_display.create({}, latexSchema.text(item.content))
                        : latexSchema.nodes.math_inline.create({}, latexSchema.text(item.content));
                    newContent.push(mathNode);
                    
                    currentIndex = item.index + item.placeholder.length;
                    hasChanges = true;
                }
                
                // 添加剩余的文本
                if (currentIndex < text.length) {
                    const textAfter = text.substring(currentIndex);
                    if (textAfter) {
                        newContent.push(latexSchema.text(textAfter));
                    }
                }
                
                if (hasChanges) {
                    return newContent;
                }
            }
            
            return [node];
        }
        
        // 步骤4：递归替换整个文档中的占位符
        function transformDoc(doc) {
            const newContent = [];
            
            doc.content.forEach(node => {
                if (node.type.name === 'paragraph') {
                    const newParagraphContent = [];
                    
                    node.content.forEach(child => {
                        const replacedNodes = replacePlaceholders(child);
                        newParagraphContent.push(...replacedNodes);
                    });
                    
                    if (newParagraphContent.length > 0) {
                        // 检查是否有块数学节点，如果有，需要将其提取出来
                        const finalContent = [];
                        let currentParagraphContent = [];
                        
                        for (const item of newParagraphContent) {
                            if (item.type.name === 'math_display') {
                                // 如果当前段落有内容，先创建段落
                                if (currentParagraphContent.length > 0) {
                                    finalContent.push(latexSchema.nodes.paragraph.create({}, currentParagraphContent));
                                    currentParagraphContent = [];
                                }
                                // 添加数学块
                                finalContent.push(item);
                            } else {
                                currentParagraphContent.push(item);
                            }
                        }
                        
                        // 如果还有剩余的段落内容
                        if (currentParagraphContent.length > 0) {
                            finalContent.push(latexSchema.nodes.paragraph.create({}, currentParagraphContent));
                        }
                        
                        newContent.push(...finalContent);
                    } else {
                        newContent.push(node);
                    }
                } else if (node.type.name === 'heading') {
                    // 处理标题节点中的占位符
                    const newHeadingContent = [];
                    
                    node.content.forEach(child => {
                        const replacedNodes = replacePlaceholders(child);
                        newHeadingContent.push(...replacedNodes);
                    });
                    
                    if (newHeadingContent.length > 0) {
                        // 标题中不应该有块数学节点，只处理行内数学
                        const finalHeadingContent = [];
                        for (const item of newHeadingContent) {
                            if (item.type.name === 'math_display') {
                                // 将块数学转换为行内数学（标题中不应该有块数学）
                                console.log("Converting block math to inline math in heading:", item.textContent);
                                finalHeadingContent.push(latexSchema.nodes.math_inline.create({}, item.content));
                            } else {
                                finalHeadingContent.push(item);
                            }
                        }
                        
                        newContent.push(latexSchema.nodes.heading.create(node.attrs, finalHeadingContent));
                    } else {
                        newContent.push(node);
                    }
                } else if (node.content && node.content.size > 0) {
                    // 处理其他有内容的节点（如列表项等）
                    const newNodeContent = [];
                    
                    node.content.forEach(child => {
                        if (child.type.name === 'paragraph') {
                            // 递归处理段落
                            const newParagraphContent = [];
                            child.content.forEach(grandchild => {
                                const replacedNodes = replacePlaceholders(grandchild);
                                newParagraphContent.push(...replacedNodes);
                            });
                            
                            if (newParagraphContent.length > 0) {
                                newNodeContent.push(latexSchema.nodes.paragraph.create({}, newParagraphContent));
                            } else {
                                newNodeContent.push(child);
                            }
                        } else {
                            newNodeContent.push(child);
                        }
                    });
                    
                    if (newNodeContent.length > 0) {
                        newContent.push(node.type.create(node.attrs, newNodeContent));
                    } else {
                        newContent.push(node);
                    }
                } else {
                    newContent.push(node);
                }
            });
            
            return latexSchema.nodes.doc.create({}, newContent);
        }
        
        const finalDoc = transformDoc(parsedDoc);
        console.log("Final doc:", finalDoc.toString());
        
        const state = EditorState.create({
            doc: finalDoc,
            schema: latexSchema,
            plugins: [
                history(),
                keymap(baseKeymap),
                keymap({
                    "Mod-z": undo,
                    "Mod-y": redo,
                    "Mod-Backspace": deleteSelection,
                }),
                ...mathPlugins,
                inputRules({ rules: markdownInputRules(latexSchema) })
            ]
        })
        
        const view = new EditorView(editorElement, {
            state: state
        })
        
        // 存储编辑器实例
        editorInstances.set(elementId, view);
        console.log("Editor instance stored for elementId:", elementId);
        console.log("Total editor instances:", editorInstances.size);
        
        // 为了兼容性，也设置全局 view
        window.view = view;
        
        return view;
    } catch (error) {
        console.error("Error initializing editor:", error);
        console.error("Error stack:", error.stack);
        return null;
    }
}

// 使用默认的序列化器，添加数学节点的序列化规则
const mathMarkdownSerializer = {
    ...defaultMarkdownSerializer.nodes,
    math_inline: (state, node) => {
        let content = node.textContent || '';
        console.log('math_inline serialization - content:', JSON.stringify(content));
        
        // 使用 state.text() 但在输出时手动处理转义
        state.text('$' + content + '$', false);
    },
    math_display: (state, node) => {
        let content = node.textContent || '';
        console.log('math_display serialization - content:', JSON.stringify(content));
        
        // 使用 state.text() 但在输出时手动处理转义
        state.text('$$\n' + content + '\n$$\n\n', false);
    }
};

// 创建序列化器实例
const customMarkdownSerializer = new MarkdownSerializer(
    mathMarkdownSerializer,
    defaultMarkdownSerializer.marks
);

function exportMarkdown(elementId = null) {
    try {
        console.log('exportMarkdown called with elementId:', elementId);
        console.log('Available editor instances:', Array.from(editorInstances.keys()));
        
        let view;
        
        if (elementId) {
            // 如果提供了 elementId，使用对应的编辑器实例
            view = editorInstances.get(elementId);
            console.log('Found view for elementId:', elementId, !!view);
        } else {
            // 否则使用全局 view（向后兼容）
            view = window.view;
            console.log('Using global view:', !!view);
        }
        
        if (!view || !view.state) {
            console.error('Editor view not found for elementId:', elementId);
            return '';
        }
        
        console.log('View state doc:', view.state.doc);
        console.log('View state doc content:', view.state.doc.content);
        
        let content = customMarkdownSerializer.serialize(view.state.doc);
        console.log('Serialized content length:', content.length);
        console.log('Serialized content preview:', content.substring(0, 200));
        
        // 修复序列化后的双重转义问题
        content = fixDoubleEscaping(content);
        console.log('Fixed content:', content);
        
        return content;
    } catch (error) {
        console.error('Error serializing markdown:', error);
        console.error('Error stack:', error.stack);
        return '';
    }
}

function fixDoubleEscaping(content) {
    // 在数学块中修复双重转义
    return content.replace(/\$\$\n([\s\S]*?)\n\$\$/g, (match, mathContent) => {
        console.log('Fixing math content:', JSON.stringify(mathContent));
        
        // 在数学内容中，将过度转义的反斜杠修复为正确的数量
        let fixed = mathContent;
        
        // 保护 LaTeX 换行符：\\\\ 后面跟着换行符或行尾（这些应该保持为 \\）
        const lineBreakPlaceholder = '___LATEX_LINEBREAK___';
        fixed = fixed.replace(/\\\\(?=\s*\n)/g, lineBreakPlaceholder);
        fixed = fixed.replace(/\\\\(?=\s*$)/gm, lineBreakPlaceholder);
        
        // 修复过度转义的 LaTeX 命令：将 \\lambda 变成 \lambda，\\frac 变成 \frac 等
        fixed = fixed.replace(/\\\\(lambda|frac|Rightarrow|Leftrightarrow|begin|end)/g, '\\$1');
        
        // 处理任何剩余的多重反斜杠（3个或更多）
        while (fixed.includes('\\\\\\')) {
            fixed = fixed.replace(/\\{3,}/g, '\\\\');
        }
        
        // 恢复 LaTeX 换行符
        fixed = fixed.replace(new RegExp(lineBreakPlaceholder, 'g'), '\\\\');
        
        console.log('Fixed math content:', JSON.stringify(fixed));
        return '$$\n' + fixed + '\n$$';
    });
}

window.initializeEditor = initializeEditor;
window.exportMarkdown = exportMarkdown;