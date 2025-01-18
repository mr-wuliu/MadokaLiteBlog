const esbuild = require("esbuild");

esbuild.build({
    entryPoints: ["wwwroot/js/prosemirror-entry.js"],
    outfile: "wwwroot/js/prosemirror.bundle.js",
    bundle: true,
    minify: true,
    format: "iife",
    globalName: "ProseMirror",
}).then(() => {
    console.log("Build complete!");
}).catch((err) => {
    console.error(err);
});
