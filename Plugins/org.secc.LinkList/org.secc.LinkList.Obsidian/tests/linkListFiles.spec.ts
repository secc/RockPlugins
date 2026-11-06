// ROCK-9362: the editor-side mirror of Utility/LinkListFiles.cs. These values
// MUST match the C# constants (LinkListFilesTests pins the same ones).
import {
    ALLOWED_EXTENSIONS,
    LINK_FILE_UPLOAD_URL,
    LINK_LIST_FILE_TYPE_GUID,
    MAX_FILE_SIZE_BYTES,
    fileExtension,
    isFileLinkUrl,
    resolveLinkHref,
    validateLinkFile
} from "../src/linkListFiles";

const listGuid = "0b6f5a1e-2c3d-4e5f-8a9b-1c2d3e4f5a6b";
const fileGuid = "7d8e9fa0-1b2c-4d3e-9f40-5a6b7c8d9e0f";
const fileUrl = `/api/secc/linklist/${listGuid}/file/${fileGuid}`;

describe("linkListFiles constants (parity with LinkListFiles.cs / LinkListGuids.cs)", () => {
    it("pins the allow-list, size cap and file type", () => {
        expect(ALLOWED_EXTENSIONS).toEqual(["pdf", "jpg", "jpeg", "png"]);
        expect(MAX_FILE_SIZE_BYTES).toBe(10 * 1024 * 1024);
        expect(LINK_LIST_FILE_TYPE_GUID).toBe("d3c59ea0-0cd7-42cd-817f-2937c31f5a86");
        // LinkListController.PostUpload's route.
        expect(LINK_FILE_UPLOAD_URL).toBe("/api/secc/linklist/upload");
    });
});

describe("fileExtension", () => {
    it.each([
        ["flyer.PDF", "pdf"],
        ["photo.jpeg", "jpeg"],
        ["archive.tar.png", "png"],
        ["noext", ""],
        ["", ""],
        [null, ""]
    ])("%s -> %s", (name, ext) => {
        expect(fileExtension(name as string | null)).toBe(ext);
    });
});

describe("validateLinkFile", () => {
    it("accepts PDF / JPG / JPEG / PNG within the cap", () => {
        for (const name of ["a.pdf", "b.JPG", "c.jpeg", "d.png"]) {
            expect(validateLinkFile({ name, size: 1024 })).toBeNull();
        }
        expect(validateLinkFile({ name: "max.pdf", size: MAX_FILE_SIZE_BYTES })).toBeNull();
    });

    it.each(["evil.svg", "page.html", "doc.docx", "x.pdf.exe", "noext"])("rejects %s", name => {
        expect(validateLinkFile({ name, size: 10 })).toMatch(/not an allowed file type/);
    });

    it("rejects oversize and empty files", () => {
        expect(validateLinkFile({ name: "big.pdf", size: MAX_FILE_SIZE_BYTES + 1 })).toMatch(/10 MB limit/);
        expect(validateLinkFile({ name: "empty.pdf", size: 0 })).toMatch(/empty/);
    });
});

describe("isFileLinkUrl / resolveLinkHref", () => {
    it("recognizes only the stored file-link shape", () => {
        expect(isFileLinkUrl(fileUrl)).toBe(true);
        expect(isFileLinkUrl(`https://rock.example.org${fileUrl}`)).toBe(false);
        expect(isFileLinkUrl(`/api/secc/linklist/${listGuid}`)).toBe(false);
        expect(isFileLinkUrl("/page/1")).toBe(false);
        expect(isFileLinkUrl(undefined)).toBe(false);
    });

    it("prefixes file links with the base URL (trailing slash tolerated)", () => {
        expect(resolveLinkHref("https://rock.example.org/", fileUrl)).toBe(`https://rock.example.org${fileUrl}`);
        expect(resolveLinkHref("", fileUrl)).toBe(fileUrl);
    });

    it("returns every other URL unchanged", () => {
        expect(resolveLinkHref("https://rock.example.org", "https://x.test")).toBe("https://x.test");
        expect(resolveLinkHref("https://rock.example.org", "/page/1")).toBe("/page/1");
        expect(resolveLinkHref("https://rock.example.org", undefined)).toBeUndefined();
    });
});
