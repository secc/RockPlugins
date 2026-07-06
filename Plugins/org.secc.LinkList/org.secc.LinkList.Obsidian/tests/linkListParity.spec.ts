import {
    extractYouTubeId,
    extractVimeoId,
    youTubeEmbedUrl,
    vimeoEmbedUrl,
    headerImageUrl,
    backgroundImageUrl,
    headerBackgroundImageUrl,
    safeUrl
} from "../src/linkListParity";

// Cases mirror the real values found in the WS2.5 production data audit:
// header-video fields hold either a full URL or a bare id.

describe("extractYouTubeId", () => {
    it.each([
        ["https://www.youtube.com/watch?v=Q4_gpN1bjmU", "Q4_gpN1bjmU"],
        ["https://youtu.be/Q4_gpN1bjmU", "Q4_gpN1bjmU"],
        ["https://www.youtube.com/embed/Q4_gpN1bjmU", "Q4_gpN1bjmU"],
        ["Q4_gpN1bjmU", "Q4_gpN1bjmU"]
    ])("%s -> %s", (input, expected) => {
        expect(extractYouTubeId(input)).toBe(expected);
    });

    it("returns null for empty/garbage", () => {
        expect(extractYouTubeId("")).toBeNull();
        expect(extractYouTubeId(null)).toBeNull();
        expect(extractYouTubeId("https://example.com/")).toBeNull();
    });

    it("builds an embed url", () => {
        expect(youTubeEmbedUrl("https://www.youtube.com/watch?v=Q4_gpN1bjmU"))
            .toBe("https://www.youtube.com/embed/Q4_gpN1bjmU");
    });
});

describe("extractVimeoId", () => {
    it.each([
        ["https://vimeo.com/870344903", "870344903"],
        ["https://vimeo.com/manage/videos/1026939681", "1026939681"],
        ["https://player.vimeo.com/video/134905817", "134905817"],
        ["886561149", "886561149"]
    ])("%s -> %s", (input, expected) => {
        expect(extractVimeoId(input)).toBe(expected);
    });

    it("returns null for empty/garbage", () => {
        expect(extractVimeoId("")).toBeNull();
        expect(extractVimeoId("not a video")).toBeNull();
    });

    it("builds an embed url", () => {
        expect(vimeoEmbedUrl("https://vimeo.com/870344903"))
            .toBe("https://player.vimeo.com/video/870344903");
    });
});

describe("image urls", () => {
    const guid = "C9F920B9-B462-447B-ABC8-CE150A135F8D";

    it("header image uses w=500&h=500&mode=crop", () => {
        expect(headerImageUrl("https://rock.secc.org", guid))
            .toBe(`https://rock.secc.org/getimage.ashx?guid=${guid}&w=500&h=500&mode=crop`);
    });

    it("background image uses w=2400 and a root-relative base", () => {
        expect(backgroundImageUrl("", guid))
            .toBe(`/getimage.ashx?guid=${guid}&w=2400`);
    });

    it("header background image uses w=2400 (wide banner)", () => {
        expect(headerBackgroundImageUrl("https://rock.secc.org", guid))
            .toBe(`https://rock.secc.org/getimage.ashx?guid=${guid}&w=2400`);
    });

    it("returns null when the value is not a guid", () => {
        expect(headerImageUrl("", "not-a-guid")).toBeNull();
        expect(backgroundImageUrl("", "")).toBeNull();
        expect(headerBackgroundImageUrl("", "")).toBeNull();
    });
});

describe("safeUrl", () => {
    it("returns the raw (un-escaped) url for valid input", () => {
        expect(safeUrl("https://se.church/give?a=1&b=2")).toBe("https://se.church/give?a=1&b=2");
    });
    it("blocks dangerous schemes", () => {
        expect(safeUrl("javascript:alert(1)")).toBe("#");
        expect(safeUrl("data:text/html,<script>alert(1)</script>")).toBe("#");
        expect(safeUrl("vbscript:msgbox(1)")).toBe("#");
        expect(safeUrl("file:///etc/passwd")).toBe("#");
        expect(safeUrl("")).toBe("#");
    });
    it("blocks schemes split by whitespace/control chars (browser strips them before parsing)", () => {
        expect(safeUrl("java\tscript:alert(1)")).toBe("#");
        expect(safeUrl("java\nscript:alert(1)")).toBe("#");
        expect(safeUrl("java\rscript:alert(1)")).toBe("#");
        expect(safeUrl("\u0000javascript:alert(1)")).toBe("#");
        expect(safeUrl("j\u0001a\u0002vascript:alert(1)")).toBe("#");
        expect(safeUrl("JAVA\tSCRIPT:alert(1)")).toBe("#");
    });
    it("treats an all-whitespace/control url as invalid", () => {
        expect(safeUrl("\t\n \u0001")).toBe("#");
    });
    it("keeps legitimate urls containing hyphens and relative paths", () => {
        expect(safeUrl("https://se.church/my-page")).toBe("https://se.church/my-page");
        expect(safeUrl("/give/now")).toBe("/give/now");
    });
});
