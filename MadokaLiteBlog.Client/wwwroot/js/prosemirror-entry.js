export * from "prosemirror-state";
export * from "prosemirror-view";
export * from "prosemirror-schema-basic";
export * from "prosemirror-history";
export * from "prosemirror-schema-list";
export * from "prosemirror-keymap";
export * from "prosemirror-commands";
export * from "prosemirror-example-setup";
export * from "prosemirror-markdown";

import {EditorState} from "prosemirror-state"
import {EditorView} from "prosemirror-view"
import {Schema, DOMParser} from "prosemirror-model"
import {schema} from "prosemirror-schema-basic"
import {addListNodes} from "prosemirror-schema-list"
import { keymap } from "prosemirror-keymap";
import { baseKeymap } from "prosemirror-commands";
import {exampleSetup} from "prosemirror-example-setup"
import { history, undo, redo } from "prosemirror-history";
import {defaultMarkdownParser, defaultMarkdownSerializer} from "prosemirror-markdown"

const mySchema = new Schema({
    nodes: addListNodes(schema.spec.nodes, "paragraph block*", "block"),
    marks: schema.spec.marks
    });

const plugins = [
    history(),
    keymap({ "Mod-z": undo, "Mod-y": redo }),
];

function initializeEditor(elementId) {
    const editorElement = document.getElementById(elementId);

    const editorState = EditorState.create( {
        schema: mySchema,
        plugins: exampleSetup({schema: mySchema})
        // plugins: plugins
      });
    
    const editorView = new EditorView(editorElement, {
        state: editorState,
    });

    window.editorView = editorView;

    return editorView;
}

function getEditorContent() {
    console.log("getEditorContent");
    const state = window.editorView.state;
    console.log(state);
    // 返回文档内容的 JSON 格式
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
            plugins: exampleSetup({schema: mySchema})
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