const Parser = require("binary-parser").Parser;

const VFile_Header = new Parser()
    .namely("VFile_Header")
    .endianess("little")
    .uint32("signature", { assert: 0x30304656 })
    .uint16("version")
    .skip(2)
    .uint32("isDispersed")
    .uint32("directoryOffset")
    .uint32("dataLength")
    .uint32("endPosition");

const DirTree_Header = new Parser()
    .namely("DirTree_Header")
    .endianess("little")
    .uint32("signature", { assert: 0x31305444 })
    .uint32("size");

const VFile_Time = new Parser()
    .endianess("little")
    .uint32("time1")
    .uint32("time2");

const VFile_Attributes = new Parser()
    .endianess("little")
    .uint32();

const VFile_Hints = new Parser()
    .endianess("little")
    .uint32("size")
    .buffer("data", { length: "size" });

const DirTree_File = new Parser()
    .namely("DirTree_File")
    .endianess("little")
    .uint32("nameLen")
    .string("name", { zeroTerminated: true })
    .nest("time", { type: VFile_Time })
    .nest("attributes", { type: VFile_Attributes })
    .uint32("size")
    .uint32("offset")
    .nest("hints", { type: VFile_Hints })
    .uint32("termChildren")
    .choice("children", {
        tag: "termChildren",
        choices: {
            0xFFFFFFFF: new Parser(),
            0: "DirTree_File"
        }
    })
    .uint32("termSibling")
    .choice("sibling", {
        tag: "termSibling",
        choices: {
            0xFFFFFFFF: new Parser(),
            0: "DirTree_File"
        }
    });

const DirTree = new Parser()
    .endianess("little")
    .nest({ type: DirTree_Header })
    .uint32("termFirst")
    .choice("firstFile", {
        tag: "termFirst",
        choices: {
            0xFFFFFFFF: new Parser(),
            0: "DirTree_File"
        }
    });

const VFile = new Parser()
    .endianess("little")
    .nest({ type: VFile_Header })
    .pointer("dirTree", {
        type: DirTree,
        offset: "directoryOffset"
    });

module.exports = {
    VFile_Header,
    DirTree_Header,
    VFile_Time,
    VFile_Hints,
    VFile_Attributes,
    DirTree_File,
    DirTree,
    VFile
};
