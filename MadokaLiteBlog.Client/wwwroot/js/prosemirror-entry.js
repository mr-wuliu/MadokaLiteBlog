export * from "prosemirror-state";
export * from "prosemirror-view";
export * from "prosemirror-schema-basic";
export * from "prosemirror-history";
export * from "prosemirror-schema-list";
export * from "prosemirror-keymap";
export * from "prosemirror-commands";
export * from "prosemirror-example-setup";
export * from "prosemirror-markdown";
export * from "@benrbray/prosemirror-math";

import {EditorState} from "prosemirror-state"
import {EditorView} from "prosemirror-view"
import {Schema, Slice} from "prosemirror-model"
import {schema} from "prosemirror-schema-basic"
import {keymap} from "prosemirror-keymap"
import { chainCommands, deleteSelection, joinBackward, selectNodeBackward} from "prosemirror-commands"
import {exampleSetup} from "prosemirror-example-setup"
import {defaultMarkdownParser, defaultMarkdownSerializer, MarkdownSerializer} from "prosemirror-markdown"
import {
    inputRules,
    textblockTypeInputRule,
    wrappingInputRule,
    InputRule,
} from "prosemirror-inputrules"
import {
    mathPlugin, 
    mathBackspaceCmd, 
    mathSerializer,
    makeInlineMathInputRule,
    makeBlockMathInputRule,
    REGEX_INLINE_MATH_DOLLARS,
    REGEX_BLOCK_MATH_DOLLARS,
    insertMathCmd
} from "@benrbray/prosemirror-math"

const mathNodes = {
    doc: {
        content: "block+"
    },
    paragraph: {
        content: "inline*",
        group: "block",
        parseDOM: [{ tag: "p" }],
        toDOM() { return ["p", 0]; }
    },
    image: {
        inline: true,
        attrs: {
            src: {},
            alt: { default: null },
            title: { default: null }
        },
        group: "inline",
        draggable: true,
        parseDOM: [{
            tag: "img[src]",
            getAttrs(dom) {
                return {
                    src: dom.getAttribute("src"),
                    title: dom.getAttribute("title"),
                    alt: dom.getAttribute("alt")
                };
            }
        }],
        toDOM(node) {
            return ["img", node.attrs];
        }
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
    text: {
        group: "inline"
    },
    hard_break: {
        inline: true,
        group: "inline",
        selectable: false,
        parseDOM: [{tag: "br"}],
        toDOM() { return ["br"] }
    },
    heading: {
        attrs: {level: {default: 1}},
        content: "inline*",
        group: "block",
        defining: true,
        parseDOM: [
            {tag: "h1", attrs: {level: 1}},
            {tag: "h2", attrs: {level: 2}},
            {tag: "h3", attrs: {level: 3}},
            {tag: "h4", attrs: {level: 4}},
            {tag: "h5", attrs: {level: 5}},
            {tag: "h6", attrs: {level: 6}}
        ],
        toDOM(node) { return ["h" + node.attrs.level, 0] }
    },
    blockquote: {
        content: "block+",
        group: "block",
        defining: true,
        parseDOM: [{tag: "blockquote"}],
        toDOM() { return ["blockquote", 0] }
    },
    ordered_list: {
        content: "list_item+",
        group: "block",
        attrs: {order: {default: 1}},
        parseDOM: [{
            tag: "ol",
            getAttrs(dom) {
                return {order: dom.hasAttribute("start") ? +dom.getAttribute("start") : 1};
            }
        }],
        toDOM(node) {
            return node.attrs.order === 1 ? ["ol", 0] : ["ol", {start: node.attrs.order}, 0];
        }
    },
    bullet_list: {
        content: "list_item+",
        group: "block",
        parseDOM: [{tag: "ul"}],
        toDOM() { return ["ul", 0] }
    },
    list_item: {
        content: "paragraph block*",
        defining: true,
        parseDOM: [{tag: "li"}],
        toDOM() { return ["li", 0] }
    },
    code_block: {
        content: "text*",
        group: "block",
        code: true,
        defining: true,
        attrs: {
            params: { default: "" }
        },
        parseDOM: [{
            tag: "pre",
            preserveWhitespace: "full",
            getAttrs: node => ({
                params: node.getAttribute("data-language") || ""
            })
        }],
        toDOM(node) {
            return ["pre", {
                "data-language": node.attrs.params
            }, ["code", 0]];
        }
    }
};

function markInputRule(regexp, markType) {
    return new InputRule(regexp, (state, match, start, end) => {
        const fullMatch = match[0];
        const content = match[2] || match[1];
        
        if (markType === schema.marks.strong) {
            if ((fullMatch.startsWith('**') && !fullMatch.endsWith('**')) ||
                (fullMatch.startsWith('__') && !fullMatch.endsWith('__'))) {
                return null;
            }
        }
        
        if (markType === schema.marks.em) {
            const before = state.doc.textBetween(Math.max(0, start - 1), start);
            if ((before === '*' && fullMatch.startsWith('*')) ||
                (before === '_' && fullMatch.startsWith('_'))) {
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
            /(__)([^_\n]+)(__)$/,
            schema.marks.strong
        ),
        
        markInputRule(
            /(?:^|[^*])(\*)([^*\n]+)(\*)$/,
            schema.marks.em
        ),
        markInputRule(
            /(?:^|[^_])(_)([^_\n]+)(_)$/,
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
        ),
    ];
}

const marks = {
    ...schema.spec.marks,
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
    code: {
        parseDOM: [{tag: "code"}],
        toDOM() { return ["code"] },
        inclusive: false
    }
};

const mySchema = new Schema({
    nodes: mathNodes,
    marks: marks
});

const inlineMathInputRule = makeInlineMathInputRule(
    REGEX_INLINE_MATH_DOLLARS, 
    mySchema.nodes.math_inline
);
const blockMathInputRule = makeBlockMathInputRule(
    REGEX_BLOCK_MATH_DOLLARS, 
    mySchema.nodes.math_display
);

const mathPlugins = [
    mathPlugin,
    keymap({
        "Mod-Space": insertMathCmd(mySchema.nodes.math_inline),
        "Backspace": chainCommands(deleteSelection, mathBackspaceCmd, joinBackward, selectNodeBackward),
    }),
    inputRules({ rules: [inlineMathInputRule, blockMathInputRule] })
];

const markdownPlugin = inputRules({
    rules: markdownInputRules(mySchema)
});

const allPlugins = [
    ...exampleSetup({schema: mySchema}),
    ...mathPlugins,
    markdownPlugin
];

function initializeEditor(elementId) {
    const editorElement = document.getElementById(elementId);

    const editorState = EditorState.create({
        schema: mySchema,
        plugins: allPlugins
    });
    
    const editorView = new EditorView(editorElement, {
        state: editorState,
        clipboardTextSerializer: (slice) => mathSerializer.serializeSlice(slice)
    });

    window.editorView = editorView;
    return editorView;
}

function getEditorContent() {
    console.log("getEditorContent");
    const state = window.editorView.state;
    console.log(state);
    return state.doc.toJSON();
}

const customMarkdownSerializer = new MarkdownSerializer({
    ...defaultMarkdownSerializer.nodes,
    math_display: (state, node) => {
        state.write('\n$$\n');
        state.write(node.textContent);
        state.write('\n$$\n');
    },
    math_inline: (state, node) => {
        state.write('$');
        state.write(node.textContent);
        state.write('$');
    }
}, defaultMarkdownSerializer.marks);

function getMarkdownContent() {
    const state = window.editorView.state;
    console.log("Editor state:", state);
    
    try {
        // 使用修正后的序列化器
        let content = customMarkdownSerializer.serialize(state.doc);
        
        // 清理多余的换行
        content = content
            .replace(/^\n+/, '')
            .replace(/\n+$/, '\n');
        
        console.log("Serialized content:", content);
        return content;
    } catch (error) {
        console.error("Serialization error:", error);
        return state.doc.textContent || '';
    }
}

function setEditorContent(markdown) {
    // FIXME
    if (!window.editorView) return;
    
    try {
        console.log("Setting content with markdown:", markdown);
        const tr = window.editorView.state.tr;
        
        if (window.editorView.state.doc.content.size > 0) {0
            tr.delete(0, window.editorView.state.doc.content.size);
        }
        console.log("开始执行");
        const isFormula = /^\$\$[\s\S]+\$\$$/m.test(markdown) || /^\$[^\$\n]+?\$$/m.test(markdown);
        console.log("isFormula: " + isFormula);
        if (isFormula) {
            // 处理公式
            const segments = markdown.split(/(\$\$[\s\S]+?\$\$|\$[^\$\n]+?\$)/g);
            console.log("segments: ", segments);
            let pos = 0;
            segments.forEach(segment => {
                console.log("segment: ", segment);
                console.log("pos: ", pos);
                
                // 获取最新的事务
                let tr = window.editorView.state.tr;

                // 打印当前文档的内容
                console.log("Current document content: ", tr.doc.toJSON());
                
                if (segment.startsWith('$$') && segment.endsWith('$$')) {
                    // 处理块级公式
                    const formula = segment.slice(2, -2).trim();
                    const node = mySchema.nodes.math_display.create(
                        null,
                        mySchema.text(formula)
                    );
                    console.log("Inserting block math node at pos: ", pos);
                    if (pos <= tr.doc.content.size) {
                        tr.insert(pos, node);
                        pos += node.nodeSize; // 更新位置
                    } else {
                        console.error(`Position ${pos} is out of range for insertion.`);
                    }
                } else if (segment.startsWith('$') && segment.endsWith('$')) {
                    // 处理行内公式
                    const formula = segment.slice(1, -1).trim();
                    const node = mySchema.nodes.math_inline.create(
                        null,
                        mySchema.text(formula)
                    );
                    console.log("Inserting inline math node at pos: ", pos);
                    if (pos <= tr.doc.content.size) {
                        tr.insert(pos, node);
                        pos += node.nodeSize; // 更新位置
                    } else {
                        console.error(`Position ${pos} is out of range for insertion.`);
                    }
                } else if (segment.trim()) {
                    const doc = defaultMarkdownParser.parse(segment);
                    console.log("doc.content:", doc.content.toJSON());
                    
                    doc.content.forEach(node => {
                        console.log("node: ", node);
                        console.log("node pos before insertion: ", pos);
                        try {
                            // 插入节点
                            if (pos <= tr.doc.content.size) {
                                tr.insert(pos, node);
                                console.log(`Node inserted at position ${pos}`);
                                pos += node.nodeSize; // 更新位置
                            } else {
                                console.error(`Position ${pos} is out of range for insertion.`);
                            }
                        } catch (error) {
                            console.error(`Error inserting node at position ${pos}:`, error);
                        }
                    });
                    
                    console.log("Updated pos after text:", pos);
                }
                
                // 在每次插入后更新tr
                tr = window.editorView.state.tr;
            });

            // 在所有插入操作后，检查最终的pos值
            console.log("Final pos after all insertions: ", pos);
        } else {
            // 处理普通文本
            const textNode = mySchema.nodes.paragraph.create(
                null,
                mySchema.text(markdown)
            );
            tr.insert(0, textNode);
        }
        
        // 应用事务
        window.editorView.dispatch(tr);
        
        console.log("Content loaded, final state:", window.editorView.state.doc.toJSON());
        
    } catch (error) {
        console.error("Error setting content:", error);
        // 错误处理：直接插入文本
        const tr = window.editorView.state.tr;
        if (window.editorView.state.doc.content.size > 0) {
            tr.delete(0, window.editorView.state.doc.content.size);
        }
        tr.insertText(markdown || '', 0);
        window.editorView.dispatch(tr);
    }
}

window.initializeEditor = initializeEditor;
window.getEditorContent = getEditorContent;
window.getMarkdownContent = getMarkdownContent;
window.setEditorContent = setEditorContent;