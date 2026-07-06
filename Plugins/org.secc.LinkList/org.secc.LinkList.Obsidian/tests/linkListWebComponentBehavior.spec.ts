/**
 * @jest-environment jsdom
 *
 * Behavior tests for <secc-link-list> — everything OTHER than happy-path
 * rendering (which linkListWebComponent.spec.ts covers): fetch error paths,
 * identifier validation, manual mode (the in-Rock Viewer-block path),
 * lifecycle cleanup (abort on disconnect, reload on reconnect), and the
 * stale-response token guard.
 */
import "../src/webComponents/linkList";
import type { LinkListBag } from "../src/types";

function baseBag(over: Partial<LinkListBag> = {}): LinkListBag {
    return { slug: "demo", title: "Demo", isPublic: true, items: [], ...over };
}

type FetchInit = { signal?: AbortSignal };

function okResponse(bag: LinkListBag): { ok: boolean; status: number; json: () => Promise<LinkListBag> } {
    return { ok: true, status: 200, json: () => Promise.resolve(bag) };
}

/** Installs a jest fetch mock and returns it for call/URL/signal assertions. */
function installFetch(impl: (url: string, init?: FetchInit) => Promise<unknown>): jest.Mock {
    const mock = jest.fn(impl);
    (global as unknown as { fetch: unknown }).fetch = mock;
    return mock;
}

async function flush(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
    await new Promise(resolve => setTimeout(resolve, 0));
    await Promise.resolve();
}

function makeElement(attrs: Record<string, string> = {}): HTMLElement {
    const el = document.createElement("secc-link-list");
    for (const [k, v] of Object.entries(attrs)) {
        el.setAttribute(k, v);
    }
    return el;
}

async function mount(attrs: Record<string, string> = {}): Promise<HTMLElement> {
    const el = makeElement({ "base-url": "https://rock.example.org", ...attrs });
    document.body.appendChild(el);
    await flush();
    return el;
}

function html(el: HTMLElement): string {
    return el.shadowRoot?.innerHTML ?? "";
}

function errorText(el: HTMLElement): string | null {
    return el.shadowRoot?.querySelector(".error")?.textContent ?? null;
}

afterEach(() => {
    document.body.innerHTML = "";
    jest.restoreAllMocks();
});

describe("fetch error paths", () => {
    it("renders 'not found' on a 404", async () => {
        installFetch(() => Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) }));
        const el = await mount({ "item-id": "missing" });
        expect(errorText(el)).toBe("Link list not found.");
    });

    it("renders the status code on a non-404 failure", async () => {
        installFetch(() => Promise.resolve({ ok: false, status: 500, json: () => Promise.resolve({}) }));
        const el = await mount({ "item-id": "demo" });
        expect(errorText(el)).toBe("Unable to load list (500).");
    });

    it("renders a generic error when fetch rejects (network failure)", async () => {
        installFetch(() => Promise.reject(new TypeError("Failed to fetch")));
        const el = await mount({ "item-id": "demo" });
        expect(errorText(el)).toBe("Unable to load link list.");
    });

    it("renders a generic error when the response body is not valid JSON", async () => {
        installFetch(() => Promise.resolve({
            ok: true,
            status: 200,
            json: () => Promise.reject(new SyntaxError("Unexpected token < in JSON"))
        }));
        const el = await mount({ "item-id": "demo" });
        expect(errorText(el)).toBe("Unable to load link list.");
    });

    it("stays silent (still loading, no error) when the fetch is aborted", async () => {
        installFetch(() => Promise.reject(new DOMException("The operation was aborted.", "AbortError")));
        const el = await mount({ "item-id": "demo" });
        expect(errorText(el)).toBeNull();
        expect(html(el)).toContain("Loading");
    });

    it("dispatches a composed secc-link-list-error event with the message", async () => {
        installFetch(() => Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) }));
        const seen: string[] = [];
        document.addEventListener("secc-link-list-error", e => {
            seen.push((e as CustomEvent<{ message: string }>).detail.message);
        });
        await mount({ "item-id": "missing" });
        expect(seen).toEqual(["Link list not found."]);
    });

    it("HTML-escapes the error message markup", async () => {
        // 599 is not a real status, but the component interpolates it into the
        // message; the assertion is that renderError output stays escaped text.
        installFetch(() => Promise.resolve({ ok: false, status: 500, json: () => Promise.resolve({}) }));
        const el = await mount({ "item-id": "demo" });
        const error = el.shadowRoot?.querySelector(".error");
        expect(error).not.toBeNull();
        // No element children — the message is text, not parsed HTML.
        expect(error?.children.length).toBe(0);
    });
});

describe("stale-response guard (loadToken)", () => {
    it("discards a slow first response after the identifier changes", async () => {
        let resolveFirst: (value: unknown) => void = () => undefined;
        const fetchMock = installFetch(url => {
            if (url.includes("/first")) {
                // Ignores the abort signal on purpose: forces the token guard
                // (not the AbortController) to be what rejects the stale data.
                return new Promise(resolve => { resolveFirst = resolve; });
            }
            return Promise.resolve(okResponse(baseBag({ title: "Second" })));
        });

        const el = await mount({ "item-id": "first" });
        el.setAttribute("item-id", "second");
        await flush();
        expect(html(el)).toContain("Second");

        resolveFirst(okResponse(baseBag({ title: "FirstStale" })));
        await flush();
        expect(html(el)).toContain("Second");
        expect(html(el)).not.toContain("FirstStale");
        expect(fetchMock).toHaveBeenCalledTimes(2);
    });
});

describe("identifier validation", () => {
    it("errors when no identifier is configured", async () => {
        installFetch(() => Promise.resolve(okResponse(baseBag())));
        const el = await mount();
        expect(errorText(el)).toBe("No identifier provided. Set item-id or slug-from-path=true.");
    });

    it("errors when the identifier exceeds 200 characters", async () => {
        installFetch(() => Promise.resolve(okResponse(baseBag())));
        const el = await mount({ "item-id": "a".repeat(201) });
        expect(errorText(el)).toBe("Identifier too long.");
    });

    it("errors on identifier characters outside [a-z0-9-]", async () => {
        const fetchMock = installFetch(() => Promise.resolve(okResponse(baseBag())));
        const el = await mount({ "item-id": "bad/../slug" });
        expect(errorText(el)).toBe("Invalid identifier.");
        expect(fetchMock).not.toHaveBeenCalled();
    });

    it("lowercases a mixed-case identifier before fetching (canonical slug form)", async () => {
        const fetchMock = installFetch(() => Promise.resolve(okResponse(baseBag())));
        await mount({ "item-id": "My-List" });
        expect(fetchMock).toHaveBeenCalledWith(
            "https://rock.example.org/api/secc/linklist/my-list",
            expect.anything()
        );
    });
});

describe("manual mode (in-Rock Viewer path)", () => {
    it("never self-fetches and renders the injected bag", async () => {
        const fetchMock = installFetch(() => Promise.resolve(okResponse(baseBag())));
        const el = makeElement({ manual: "true" });
        document.body.appendChild(el);
        (el as HTMLElement & { listData: LinkListBag | null }).listData = baseBag({ title: "Injected" });
        await flush();
        expect(html(el)).toContain("Injected");
        expect(fetchMock).not.toHaveBeenCalled();
    });

    it("renders a bag injected BEFORE the element is connected", async () => {
        const fetchMock = installFetch(() => Promise.resolve(okResponse(baseBag())));
        const el = makeElement({ manual: "true" });
        (el as HTMLElement & { listData: LinkListBag | null }).listData = baseBag({ title: "PreConnect" });
        document.body.appendChild(el);
        await flush();
        expect(html(el)).toContain("PreConnect");
        expect(fetchMock).not.toHaveBeenCalled();
    });

    it("shows loading while waiting for injection", async () => {
        const fetchMock = installFetch(() => Promise.resolve(okResponse(baseBag())));
        const el = makeElement({ manual: "true" });
        document.body.appendChild(el);
        await flush();
        expect(html(el)).toContain("Loading");
        expect(fetchMock).not.toHaveBeenCalled();
    });

    it("renders 'not found' when the host injects null", async () => {
        installFetch(() => Promise.resolve(okResponse(baseBag())));
        const el = makeElement({ manual: "true" });
        document.body.appendChild(el);
        (el as HTMLElement & { listData: LinkListBag | null }).listData = null;
        await flush();
        expect(errorText(el)).toBe("Link list not found.");
    });
});

describe("lifecycle cleanup", () => {
    it("aborts the in-flight fetch on disconnect and renders nothing after removal", async () => {
        let capturedSignal: AbortSignal | undefined;
        installFetch((_url, init) => {
            capturedSignal = init?.signal;
            return new Promise(() => undefined); // never resolves
        });

        const el = await mount({ "item-id": "demo" });
        expect(capturedSignal?.aborted).toBe(false);

        el.remove();
        expect(capturedSignal?.aborted).toBe(true);
        expect(html(el)).toContain("Loading"); // unchanged; no late render
    });

    it("reloads when reconnected", async () => {
        const fetchMock = installFetch(() => Promise.resolve(okResponse(baseBag({ title: "Reloaded" }))));
        const el = await mount({ "item-id": "demo" });
        expect(fetchMock).toHaveBeenCalledTimes(1);

        el.remove();
        document.body.appendChild(el);
        await flush();
        expect(fetchMock).toHaveBeenCalledTimes(2);
        expect(html(el)).toContain("Reloaded");
    });
});
