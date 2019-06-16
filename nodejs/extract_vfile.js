const VFile = require("./parser/vfile").VFile;
const fs = require("fs");
const path = require("path");

if (process.argv.length < 3) {
    console.error("usage: node extract_vfile.js <path_to_vfile>");
    return;
}

const buffer = fs.readFileSync(process.argv[2]);
const vfile = VFile.parse(buffer);

fs.writeFileSync(`${path.basename(process.argv[2])}.json`, JSON.stringify(vfile, null, "  "));

if (typeof vfile.dirTree.firstFile.name === "undefined")
    throw new Error("No content in vfile");
if (typeof vfile.dirTree.firstFile.sibling.name !== "undefined")
    throw new Error("Invalid root sibling");
if (vfile.dirTree.firstFile.name === "")
    vfile.dirTree.firstFile.name = path.basename(process.argv[2], ".act");
extractSiblings(vfile.dirTree.firstFile, ".");

function extractSiblings(node, outPath) {
    while(typeof node.name !== "undefined") {
        const hasChildren = ((node.attributes & 2) > 0);
        if (hasChildren && node.size > 0)
            throw new Error("Invalid directory with file content");

        if (hasChildren) {
            const childPath = path.join(outPath, node.name);
            try { fs.mkdirSync(childPath); } catch {}
            extractSiblings(node.children, childPath);
        }
        else {
            fs.writeFileSync(path.join(outPath, node.name),
                buffer.slice(node.offset, node.offset + node.size));
        }

        node = node.sibling;
    }
}
