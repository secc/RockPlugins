// ROCK-9362: uploaded link files (PDF / JPG / PNG).
//
// The editor-side mirror of the server's Utility/LinkListFiles.cs: the same
// allow-list and size cap, used here only for early feedback (a bad file is
// refused before it is uploaded). The server enforces everything - extension,
// reported MIME type, size and magic bytes on upload, and again (plus who
// uploaded it) on save - so these constants are a convenience. Keep them in step
// with LinkListFiles.cs; tests/linkListFiles.spec.ts pins the values.

/** The "Link List File" BinaryFileType (LinkListGuids.BinaryFileTypeLinkListFile, migration 013). */
export const LINK_LIST_FILE_TYPE_GUID = "d3c59ea0-0cd7-42cd-817f-2937c31f5a86";

/**
 * The plugin's upload endpoint (LinkListController.PostUpload). Used as the
 * baseUrl of Rock's uploadBinaryFile helper. Requires EDIT on the Link List
 * File type (admins, Rock Edit Access, Staff Like Workers by default).
 */
export const LINK_FILE_UPLOAD_URL = "/api/secc/linklist/upload";

/** 50 MB - LinkListFiles.MaxFileSizeBytes (sized from production; see there). */
export const MAX_FILE_SIZE_BYTES = 50 * 1024 * 1024;

/** Allowed extensions (lowercase, no dot). */
export const ALLOWED_EXTENSIONS = ["pdf", "jpg", "jpeg", "png"];

/** The file picker's accept attribute. */
export const FILE_ACCEPT = ".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png";

/** Path prefix of a stored file link: /api/secc/linklist/{listGuid}/file/{fileGuid}. */
const FILE_URL_PATTERN = /^\/api\/secc\/linklist\/[0-9a-f-]{36}\/file\/[0-9a-f-]{36}$/i;

/** Lowercase extension without the dot, or "". */
export function fileExtension(name: string | null | undefined): string {
    const match = /\.([^./\\]+)$/.exec((name ?? "").trim());
    return match ? match[1].toLowerCase() : "";
}

/**
 * Early client-side check for a picked file. Returns an error message, or
 * null when the file may be uploaded.
 */
export function validateLinkFile(file: { name: string; size: number }): string | null {
    if (!ALLOWED_EXTENSIONS.includes(fileExtension(file.name))) {
        return `"${file.name}" is not an allowed file type. Upload a PDF, JPG, or PNG file.`;
    }
    if (file.size <= 0) {
        return `"${file.name}" is empty.`;
    }
    if (file.size > MAX_FILE_SIZE_BYTES) {
        return `"${file.name}" is larger than the ${MAX_FILE_SIZE_BYTES / (1024 * 1024)} MB limit.`;
    }
    return null;
}

/** True when a link URL is a stored uploaded-file URL (server-relative). */
export function isFileLinkUrl(url: string | null | undefined): boolean {
    return FILE_URL_PATTERN.test((url ?? "").trim());
}

/**
 * The href to render for a link. Uploaded-file links are stored RELATIVE to
 * Rock (so a database copied between environments keeps working); an external
 * embed must point them at Rock, so they get the component's base URL. Every
 * other URL is returned unchanged. The result still goes through safeUrl().
 */
export function resolveLinkHref(baseUrl: string | null | undefined, url: string | null | undefined): string | null | undefined {
    if (!isFileLinkUrl(url)) {
        return url;
    }
    const root = (baseUrl || "").replace(/\/$/, "");
    return `${root}${(url ?? "").trim()}`;
}
