/**
 * @jest-environment jsdom
 *
 * ROCK-7164: click-analytics instrumentation of <secc-link-list> — the
 * data-link-id markup, the delegated shadow-root click listener, and the
 * sendBeacon call (URL, body, single-fire, graceful absence).
 */
import "../src/webComponents/linkList";
import type { LinkItemBag, LinkListBag } from "../src/types";

const LIST_GUID = "0f8fad5b-d9cb-469f-a165-70867728950e";
const LINK_GUID = "7c9e6679-7425-40de-944b-e07fc1f90ae7";
const OTHER_GUID = "1b9d6bcd-bbfd-4b2d-9b5d-ab8dfbbd4bed";

function link(over: Partial<LinkItemBag> = {}): LinkItemBag {
    return { itemType: "link", text: "Give", url: "#", target: "_self", guid: LINK_GUID, matrixItemGuid: LINK_GUID, order: 0, ...over };
}

function baseBag(over: Partial<LinkListBag> = {}): LinkListBag {
    return { slug: "demo", title: "Demo", isPublic: true, guid: LIST_GUID, items: [link()], ...over };
}

function mockFetch(bag: LinkListBag): jest.Mock {
    const mock = jest.fn(() => Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(bag) }));
    (global as unknown as { fetch: unknown }).fetch = mock;
    return mock;
}

function mockBeacon(): jest.Mock {
    const beacon = jest.fn(() => true);
    Object.defineProperty(navigator, "sendBeacon", { value: beacon, configurable: true, writable: true });
    return beacon;
}

function clearBeacon(): void {
    Object.defineProperty(navigator, "sendBeacon", { value: undefined, configurable: true, writable: true });
}

async function flush(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
    await new Promise(resolve => setTimeout(resolve, 0));
    await Promise.resolve();
}

async function mountFetch(bag: LinkListBag): Promise<HTMLElement> {
    mockFetch(bag);
    const el = document.createElement("secc-link-list");
    el.setAttribute("item-id", "demo");
    el.setAttribute("base-url", "https://rock.example.org");
    document.body.appendChild(el);
    await flush();
    return el;
}

async function mountManual(bag: LinkListBag): Promise<HTMLElement> {
    const el = document.createElement("secc-link-list");
    el.setAttribute("manual", "true");
    document.body.appendChild(el);
    (el as HTMLElement & { listData: LinkListBag | null }).listData = bag;
    await flush();
    return el;
}

function clickAnchor(el: HTMLElement, selector = "a[part=link]"): void {
    const anchor = el.shadowRoot?.querySelector(selector);
    expect(anchor).not.toBeNull();
    anchor!.dispatchEvent(new MouseEvent("click", { bubbles: true, composed: true }));
}

afterEach(() => {
    document.body.innerHTML = "";
    clearBeacon();
    jest.restoreAllMocks();
});

describe("data-link-id markup", () => {
    it("emits the matrix row guid on a top-level link", async () => {
        mockBeacon();
        const el = await mountFetch(baseBag());
        const anchor = el.shadowRoot?.querySelector("a[part=link]");
        expect(anchor?.getAttribute("data-link-id")).toBe(LINK_GUID);
    });

    it("emits it on accordion-child links", async () => {
        mockBeacon();
        const el = await mountFetch(baseBag({
            items: [
                { itemType: "section", text: "Info", order: 0 },
                link({ guid: OTHER_GUID, matrixItemGuid: OTHER_GUID, indentLevel: 1, order: 1 })
            ]
        }));
        const anchor = el.shadowRoot?.querySelector(".accord-link");
        expect(anchor?.getAttribute("data-link-id")).toBe(OTHER_GUID);
    });

    it("emits it on the featured link", async () => {
        mockBeacon();
        const el = await mountFetch(baseBag({ items: [link({ isFeatured: true })] }));
        const anchor = el.shadowRoot?.querySelector("a.link-btn.featured");
        expect(anchor?.getAttribute("data-link-id")).toBe(LINK_GUID);
    });

    it("omits the attribute when the item has no guid", async () => {
        const beacon = mockBeacon();
        const el = await mountFetch(baseBag({ items: [link({ guid: undefined, matrixItemGuid: undefined })] }));
        const anchor = el.shadowRoot?.querySelector("a[part=link]");
        expect(anchor?.hasAttribute("data-link-id")).toBe(false);
        clickAnchor(el);
        expect(beacon).not.toHaveBeenCalled();
    });
});

describe("click beacon", () => {
    it("fires exactly one beacon with the list guid URL and row guid body (fetch mode)", async () => {
        const beacon = mockBeacon();
        const el = await mountFetch(baseBag());
        clickAnchor(el);
        expect(beacon).toHaveBeenCalledTimes(1);
        expect(beacon).toHaveBeenCalledWith(
            `https://rock.example.org/api/secc/linklist/${LIST_GUID}/click`,
            JSON.stringify({ matrixItemGuid: LINK_GUID })
        );
    });

    it("uses window.location.origin in manual (in-Rock) mode", async () => {
        const beacon = mockBeacon();
        const el = await mountManual(baseBag());
        clickAnchor(el);
        expect(beacon).toHaveBeenCalledTimes(1);
        expect((beacon.mock.calls[0] as string[])[0])
            .toBe(`${window.location.origin}/api/secc/linklist/${LIST_GUID}/click`);
    });

    it("does not fire for accordion triggers", async () => {
        const beacon = mockBeacon();
        const el = await mountFetch(baseBag({
            items: [
                { itemType: "section", text: "Info", order: 0 },
                link({ indentLevel: 1, order: 1 })
            ]
        }));
        const trigger = el.shadowRoot?.querySelector(".accord-trigger");
        expect(trigger).not.toBeNull();
        trigger!.dispatchEvent(new MouseEvent("click", { bubbles: true, composed: true }));
        expect(beacon).not.toHaveBeenCalled();
    });

    it("does not throw when sendBeacon is unavailable", async () => {
        clearBeacon();
        const el = await mountFetch(baseBag());
        expect(() => clickAnchor(el)).not.toThrow();
    });

    it("still fires exactly once per click after a re-render (listener attached once)", async () => {
        const beacon = mockBeacon();
        const el = await mountManual(baseBag());
        // Re-inject: renderList runs again, swapping innerHTML on the same root.
        (el as HTMLElement & { listData: LinkListBag | null }).listData = baseBag({ title: "Again" });
        await flush();
        clickAnchor(el);
        expect(beacon).toHaveBeenCalledTimes(1);
    });

    it("does not fire when the bag has no list guid", async () => {
        const beacon = mockBeacon();
        const el = await mountManual(baseBag({ guid: undefined }));
        clickAnchor(el);
        expect(beacon).not.toHaveBeenCalled();
    });
});
