const path = require("path");
const typescript = require("@rollup/plugin-typescript");

module.exports = {
    input: path.resolve(__dirname, "src/webComponents/linkList.ts"),
    output: {
        file: path.resolve(__dirname, "dist/linkList.webcomponent.js"),
        format: "iife",
        sourcemap: true
    },
    plugins: [
        typescript({
            tsconfig: path.resolve(__dirname, "src/tsconfig.json")
        })
    ]
};
