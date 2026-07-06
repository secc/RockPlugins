# org.secc.SmsCapture — Dev SMS Capture Transport ("Papercut for SMS")

A Rock RMS **SMS transport plugin** that captures fully rendered outbound SMS messages to a
database table instead of sending them — the SMS equivalent of Papercut SMTP. The transport
reports success to Rock's communication pipeline so recipient statuses advance to
Delivered/Sent exactly as they would in production, and a Papercut-style "SMS Capture Inbox"
block shows everything that would have been sent.

> **DEV / TESTING ONLY.** This transport never delivers a message. It contains no Twilio SDK
> calls and no outbound HTTP whatsoever. It is deliberately kept in its own assembly
> (`org.secc.SmsCapture.dll`) so it can be deployed to DEV without ever shipping to production.
> Fail-closed: if it *is* accidentally deployed and selected on production, messages are
> captured, not sent — annoying, but safe in the right direction.

## What gets captured

Every SMS Rock attempts to send through the SMS medium — bulk communications, system
communications, SMS conversation replies, workflow SMS actions, and nightly jobs — is written
to `_org_secc_SmsCapture_CapturedSms` with:

| Field | Notes |
|---|---|
| FromNumber / ToNumber | as they would have gone to Twilio |
| RecipientPersonAliasId | FK to PersonAlias when resolvable |
| Body | fully rendered message (Lava already resolved) |
| AttachmentBinaryFileIds | comma-delimited BinaryFile ids (references only; media is not downloaded) |
| CommunicationId / CommunicationRecipientId | when applicable (plain ids, no FK, so communication cleanup jobs are never blocked) |
| Source | which `Send()` overload produced it: `Communication` (bulk/queued path) or `RockMessage` (immediate path) |
| CreatedDateTime | capture time |

The transport is sync-only (it does not implement `IAsyncTransport`); Rock's
`MediumComponent` automatically falls back to the synchronous `Send` overloads on every
pipeline path, so nothing is missed.

Component attributes:

- **Enable Logging** (default: true) — writes a compact line to the Rock log
  (`COMMUNICATIONS` domain) per captured message.
- **Max Captured Messages** (default: 5000) — after each send, the oldest rows beyond this
  cap are trimmed. Set to 0 to disable trimming.

## Manual wiring steps (DEV ONLY)

1. Deploy the plugin to all DEV web farm nodes: `org.secc.SmsCapture.dll` (+ `.pdb`) to
   `RockWeb\bin`, and `org_secc\SmsCapture\` block files to `RockWeb\Plugins\org_secc\SmsCapture\`.
   The plugin migration creates the capture table automatically on startup.
2. **Admin Tools → Communications → Communication Transports** → confirm **SMS Capture**
   appears; set its attributes and mark it **Active**.
3. **Admin Tools → Communications → Communication Mediums → SMS** → set the Transport
   Container selection to **SMS Capture**.
   ⚠️ **DEV ONLY — never make this change on production.** ⚠️
4. Create an internal admin page (e.g., under Admin Tools → Communications) and add the
   **SMS Capture Inbox** block (category *SECC > Communication*).
5. **Post-clone checklist:** after any prod→DEV database restore, re-verify the SMS medium's
   transport is **SMS Capture** *before* enabling any jobs. The medium's transport setting
   lives in the database (`Attribute`/`AttributeValue`) and **will revert to Twilio on
   clone** — same class of problem as the OAuth Acceptable Domain setting that carried over
   after a previous clone. Add this to the existing post-clone sanitization checklist doc.
   Blanking the Twilio SID/Auth Token attribute values on DEV is a good belt-and-suspenders
   measure while you're in there.

## Verifying no Twilio traffic

- The `org.secc.SmsCapture` project has **no** Twilio package reference, no `HttpClient`,
  and no outbound calls of any kind — capture rows and log lines are the only side effects.
- On DEV, blank the Twilio transport's SID/Auth Token attributes; test sends still succeed
  (they never touch Twilio).

## Phase 2 (not built)

An inbound simulator (POSTing a synthetic Twilio webhook payload to Rock's SMS pipeline to
test keyword responses and two-way conversations) was deliberately left out of scope.

---

**Last updated:** 2026-07-06
