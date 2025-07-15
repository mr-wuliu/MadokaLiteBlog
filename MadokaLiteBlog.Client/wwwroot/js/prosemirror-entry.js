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
    
    // 测试解析
    const testInline = "$ x = 1 $";
    const testBlock = "$$ \\frac{1}{2} $$";
    
    console.log("Test inline parsing:", md.parse(testInline));
    console.log("Test block parsing:", md.parse(testBlock));
    
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

// 检查 mathPlugin 是否覆盖了我们的节点定义
console.log("latexSchema.nodes.math_display:", latexSchema.nodes.math_display);
console.log("latexSchema.nodes.math_inline:", latexSchema.nodes.math_inline);

const mathPlugins = [
    mathPlugin,
    keymap({
        "Mod-Space": insertMathCmd(latexSchema.nodes.math_inline),
        "Backspace": chainCommands(deleteSelection, mathBackspaceCmd, joinBackward, selectNodeBackward),
    }),
    inputRules({ rules: [inlineMathInputRule, blockMathInputRule] })
];

console.log("mathPlugins:", mathPlugins);

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

// 简化的数学节点创建函数
function createMathDoc(processedContent, schema) {
    const lines = processedContent.split('\n');
    const content = [];
    
    for (const line of lines) {
        if (line.trim() === '') {
            continue;
        }
        
        // 检查是否是块公式
        const blockMathMatch = line.match(/\[BLOCK_MATH\](.*?)\[\/BLOCK_MATH\]/);
        if (blockMathMatch) {
            const mathContent = blockMathMatch[1];
            console.log("Creating math_display node with content:", mathContent);
            const mathNode = schema.nodes.math_display.create({}, schema.text(mathContent));
            content.push(mathNode);
            continue;
        }
        
        // 处理包含行内公式的段落
        const inlineMathRegex = /\[INLINE_MATH\](.*?)\[\/INLINE_MATH\]/g;
        if (inlineMathRegex.test(line)) {
            const paragraphContent = [];
            let lastIndex = 0;
            let match;
            
            // 重置正则表达式
            inlineMathRegex.lastIndex = 0;
            
            while ((match = inlineMathRegex.exec(line)) !== null) {
                // 添加公式前的文本
                if (match.index > lastIndex) {
                    const textBefore = line.substring(lastIndex, match.index);
                    if (textBefore) {
                        paragraphContent.push(schema.text(textBefore));
                    }
                }
                
                // 添加数学节点
                const mathContent = match[1];
                console.log("Creating math_inline node with content:", mathContent);
                const mathNode = schema.nodes.math_inline.create({}, schema.text(mathContent));
                paragraphContent.push(mathNode);
                
                lastIndex = match.index + match[0].length;
            }
            
            // 添加公式后的文本
            if (lastIndex < line.length) {
                const textAfter = line.substring(lastIndex);
                if (textAfter) {
                    paragraphContent.push(schema.text(textAfter));
                }
            }
            
            const paragraph = schema.nodes.paragraph.create({}, paragraphContent);
            content.push(paragraph);
        } else {
            // 普通段落
            const paragraph = schema.nodes.paragraph.create({}, schema.text(line));
            content.push(paragraph);
        }
    }
    
    return schema.nodes.doc.create({}, content);
}

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
        // 手动处理数学公式
        let processedContent = markdown_content;
        
        // 处理块公式 $$...$$
        processedContent = processedContent.replace(/\$\$\s*\n?([\s\S]*?)\n?\s*\$\$/g, (match, content) => {
            console.log("Found block math:", content.trim());
            return `[BLOCK_MATH]${content.trim()}[/BLOCK_MATH]`;
        });
        
        // 处理行内公式 $...$
        processedContent = processedContent.replace(/\$([^$\n]+)\$/g, (match, content) => {
            console.log("Found inline math:", content.trim());
            return `[INLINE_MATH]${content.trim()}[/INLINE_MATH]`;
        });
        
        console.log("Processed content:", processedContent);
        
        // 先用普通的 markdown 解析器解析
        const tokens = latexParser.parse(processedContent);
        console.log("tokens:", tokens.toString());
        
        // 手动创建包含数学节点的文档
        const finalDoc = createMathDoc(processedContent, latexSchema);
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
        
        // 检查最终的 schema
        console.log("Final schema nodes:", state.schema.nodes);
        console.log("Final math_display node:", state.schema.nodes.math_display);
        console.log("Final math_display toDOM:", state.schema.nodes.math_display.spec.toDOM);
        
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

// 自定义序列化器，避免使用有问题的 mathSerializer
const mathMarkdownSerializer = {
    ...defaultMarkdownSerializer.nodes,
    math_inline: (state, node) => {
        // 尝试从节点的不同属性获取内容
        let content = '';
        
        // 方法1：从textContent获取
        if (node.textContent) {
            content = node.textContent;
        }
        // 方法2：从attrs获取
        else if (node.attrs && node.attrs.content) {
            content = node.attrs.content;
        }
        // 方法3：从子节点获取
        else if (node.content && node.content.size > 0) {
            content = node.textContent || '';
        }
        
        console.log('math_inline original content:', content);
        console.log('math_inline node:', node);
        console.log('math_inline node.attrs:', node.attrs);
        console.log('math_inline node.content:', node.content);
        
        // 检查是否已经有反斜杠转义问题
        if (content.includes('\\\\')) {
            // 递归替换所有重复的反斜杠，直到没有变化
            let prevContent;
            do {
                prevContent = content;
                content = content.replace(/\\\\/g, '\\');
            } while (prevContent !== content);
        }
        
        console.log('math_inline processed content:', content);
        state.text('$' + content + '$');
    },
    math_display: (state, node) => {
        // 尝试从节点的不同属性获取内容
        let content = '';
        
        // 方法1：从textContent获取
        if (node.textContent) {
            content = node.textContent;
        }
        // 方法2：从attrs获取
        else if (node.attrs && node.attrs.content) {
            content = node.attrs.content;
        }
        // 方法3：从子节点获取
        else if (node.content && node.content.size > 0) {
            content = node.textContent || '';
        }
        
        console.log('math_display original content:', content);
        console.log('math_display node:', node);
        console.log('math_display node.attrs:', node.attrs);
        console.log('math_display node.content:', node.content);
        
        // 检查是否已经有反斜杠转义问题
        if (content.includes('\\\\')) {
            // 递归替换所有重复的反斜杠，直到没有变化
            let prevContent;
            do {
                prevContent = content;
                content = content.replace(/\\\\/g, '\\');
            } while (prevContent !== content);
        }
        
        console.log('math_display processed content:', content);
        state.text('$$\n' + content + '\n$$\n\n');
    },
    math_inline_double: (state, node) => {
        // 尝试从节点的不同属性获取内容
        let content = '';
        
        // 方法1：从textContent获取
        if (node.textContent) {
            content = node.textContent;
        }
        // 方法2：从attrs获取
        else if (node.attrs && node.attrs.content) {
            content = node.attrs.content;
        }
        // 方法3：从子节点获取
        else if (node.content && node.content.size > 0) {
            content = node.textContent || '';
        }
        
        console.log('math_inline_double original content:', content);
        console.log('math_inline_double node:', node);
        console.log('math_inline_double node.attrs:', node.attrs);
        console.log('math_inline_double node.content:', node.content);
        
        // 检查是否已经有反斜杠转义问题
        if (content.includes('\\\\')) {
            // 递归替换所有重复的反斜杠，直到没有变化
            let prevContent;
            do {
                prevContent = content;
                content = content.replace(/\\\\/g, '\\');
            } while (prevContent !== content);
        }
        
        console.log('math_inline_double processed content:', content);
        state.text('$$' + content + '$$');
    }
};

// 创建正确的序列化器实例
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
        
        // 在返回之前清理所有重复的反斜杠
        if (content.includes('\\\\')) {
            console.log('Found double backslashes, cleaning...');
            let prevContent;
            do {
                prevContent = content;
                content = content.replace(/\\\\/g, '\\');
            } while (prevContent !== content);
            console.log('Cleaned content preview:', content.substring(0, 200));
        }
        
        return content;
    } catch (error) {
        console.error('Error serializing markdown:', error);
        console.error('Error stack:', error.stack);
        return '';
    }
}

window.initializeEditor = initializeEditor;
window.exportMarkdown = exportMarkdown;