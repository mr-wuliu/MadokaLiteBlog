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
import {Schema} from "prosemirror-model"
import {schema} from "prosemirror-schema-basic"
import {keymap} from "prosemirror-keymap"
import { chainCommands, deleteSelection, joinBackward, selectNodeBackward} from "prosemirror-commands"
import {exampleSetup} from "prosemirror-example-setup"
import {defaultMarkdownParser, defaultMarkdownSerializer} from "prosemirror-markdown"
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

function getMarkdownContent() {
    const state = window.editorView.state;
    return defaultMarkdownSerializer.serialize(state.doc);
}

function setEditorContent(markdown) {
    if (!window.editorView) return;
    
    try {
        const doc = defaultMarkdownParser.parse(markdown || '');
        
        const newState = EditorState.create({
            doc,
            schema: mySchema,
            plugins: allPlugins
        });

        window.editorView.updateState(newState);
    } catch (error) {
        console.error("Error parsing markdown:", error);
    }
}

window.initializeEditor = initializeEditor;
window.getEditorContent = getEditorContent;
window.getMarkdownContent = getMarkdownContent;
window.setEditorContent = setEditorContent;