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
import { schema as basicSchema } from "prosemirror-schema-basic"; // 导入基本 schema
import {keymap} from "prosemirror-keymap"
import { chainCommands, deleteSelection, joinBackward, selectNodeBackward} from "prosemirror-commands"
import { MarkdownParser } from "prosemirror-markdown";
import { history, undo, redo } from "prosemirror-history";
import {defaultMarkdownSerializer} from "prosemirror-markdown"
import { mergeDelimiters, inline as texmathInline, block as texmathBlock } from 'markdown-it-texmath';
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
    mathSerializer,
    makeInlineMathInputRule,
    makeBlockMathInputRule,
    REGEX_INLINE_MATH_DOLLARS,
    REGEX_BLOCK_MATH_DOLLARS,
    insertMathCmd,
} from "@benrbray/prosemirror-math"

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
    math_inline_double: {
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
  
    const delimiters = mergeDelimiters(['dollars', 'beg_end']);
  
    // inject rules into markdown-it
    delimiters.inline.forEach((baseRule) => {
      const rule = { ...baseRule };
      if ('outerSpace' in rule) rule.outerSpace = true;
      md.inline.ruler.before('escape', rule.name, texmathInline(rule));
    });
    delimiters.block.forEach((rule) => {
      md.block.ruler.before('fence', rule.name, texmathBlock(rule));
    });
    console.log("markdownItWithMath:");
    console.log(md);
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
        code_block: {block: "code_block", noCloseToken: true},
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
        math_inline: { block: 'math_inline', noCloseToken: true },
        math_inline_double: { block: 'math_inline_double', noCloseToken: true },
        math_block: { block: 'math_display', noCloseToken: true },
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
        ),
    ];
} 

function initializeEditor(elementId, markdown_content) {
    const editorElement  = document.getElementById(elementId);
    console.log("markdown_content:");
    console.log(markdown_content);
    // 输出分词器的结果
    const tokens = latexParser.parse(markdown_content);
    console.log("tokens:");
    console.log(tokens.toString());
    const state = EditorState.create({
        doc: latexParser.parse(markdown_content),
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
    const view = new EditorView(editorElement , {
        state: state,
        clipboardTextSerializer: (slice) => { return mathSerializer.serializeSlice(slice) },
    })
    
    window.view = view;
    return view;
}

function exportMarkdown() {
    if (!window.view) return "";
    const doc = window.view.state.doc;
    return defaultMarkdownSerializer.serialize(doc);
}

window.initializeEditor = initializeEditor;
window.exportMarkdown = exportMarkdown;