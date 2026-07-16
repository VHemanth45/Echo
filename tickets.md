# Simplified Echo MVP — Ticket Breakdown

**Estimate scale:** focused engineering effort for one experienced developer, including automated verification. Estimates exclude gateway deployment, NVIDIA credentials, GitHub permissions, and external review.

## 1. Bootstrap the Windows app and NIM gateway

**Effort:** 1–2 engineer-days

**Blocked by:** None — can start immediately.

**What it delivers:** Developers can build a launchable .NET 8 WinUI 3 Echo application and a minimal ASP.NET Core gateway with a health endpoint and shared request/response contracts.

**Acceptance criteria:**

- [x] The desktop application launches and the gateway reports healthy.
- [x] Profile-generation and rewrite contract types compile across client and gateway.
- [x] A runnable test project is present.

## 2. Add one-time onboarding and protected local profile storage

**Effort:** 2–3 engineer-days

**Blocked by:** 1 — Bootstrap the Windows app and NIM gateway.

**What it delivers:** A user can review the privacy notice, grant cloud consent, supply three to ten samples, and have consent plus the generated profile stored locally with DPAPI.

**Acceptance criteria:**

- [x] Onboarding validates sample count and blocks remote processing before consent.
- [x] The user can view the generated profile after onboarding.
- [x] Only consent and generated profile persist locally; samples are not retained.
- [x] Tests cover consent, sample validation, and protected storage.

## 3. Implement managed NVIDIA NIM profile generation

**Effort:** 2–3 engineer-days

**Blocked by:** 1 — Bootstrap the Windows app and NIM gateway.

**What it delivers:** The gateway turns onboarding samples into a structured voice profile using its configured NVIDIA NIM model without exposing its credential.

**Acceptance criteria:**

- [x] NIM credential and model are gateway configuration only.
- [x] A profile response covers tone, rhythm, vocabulary, formality, recurring patterns, and avoidance tendencies.
- [x] Mocked-NIM integration tests cover success and provider failure.
- [x] Persistent logs exclude samples, generated profiles, and credentials.

## 4. Build the safe global clipboard rewrite primitive

**Effort:** 2–3 engineer-days

**Blocked by:** 1 — Bootstrap the Windows app and NIM gateway.

**What it delivers:** A background Echo process can use `Ctrl+Alt+E` to copy a selection, obtain a fake rewrite, paste it back, and restore the prior clipboard contents.

**Acceptance criteria:**

- [x] Echo is single-instance and relaunching it brings its window to the foreground.
- [x] The shortcut is active while Echo runs in the background and is disabled after explicit exit.
- [x] No selection, copy timeout, unsupported target, and rewrite failure leave target text unchanged.
- [x] Clipboard behavior is tested through faked system adapters.

## 5. Add managed NIM rewrite behavior

**Effort:** 2–3 engineer-days

**Blocked by:** 3 — Implement managed NVIDIA NIM profile generation.

**What it delivers:** The gateway rewrites selected text with a saved voice profile through NVIDIA NIM and returns clear, normalized success or failure results.

**Acceptance criteria:**

- [x] Requests exceeding 2,000 words are rejected before calling NIM.
- [x] Prompting requires meaning, fact, language, link, and practical-format retention with no invented details.
- [x] Gateway logs exclude selected text, voice profiles, and generated rewrites.
- [x] Unit and mock-provider tests cover limits, request construction, and failures.

## 6. Ship end-to-end onboarding and rewrite

**Effort:** 2–3 engineer-days

**Blocked by:** 2 — Add one-time onboarding and protected local profile storage; 4 — Build the safe global clipboard rewrite primitive; 5 — Add managed NIM rewrite behavior.

**What it delivers:** A user who completes onboarding can select text in a supported editor, press `Ctrl+Alt+E`, and receive an in-place rewrite in their saved voice.

**Acceptance criteria:**

- [x] The client sends onboarding samples only for profile generation and sends selected text plus saved profile only for rewriting.
- [x] Successful rewrites paste in place and restore the old clipboard; all failures display a concise notification without changing text.
- [x] The complete flow is demonstrable in Notepad and passes an orchestration-level automated test.

## 7. Create the static download site and manual release guide

**Effort:** 1–2 engineer-days

**Blocked by:** None — can start immediately.

**What it delivers:** A visitor can understand Echo, its privacy boundary, and Windows requirements, then follow a link to download the current executable from GitHub Releases.

**Acceptance criteria:**

- [x] The static site includes product summary, privacy note, Windows requirements, and latest-release link.
- [x] The site builds for GitHub Pages deployment.
- [x] Project documentation explains manual executable publication and updating the release link.

## 8. Run MVP acceptance checks

**Effort:** 1–2 engineer-days

**Blocked by:** 6 — Ship end-to-end onboarding and rewrite; 7 — Create the static download site and manual release guide.

**What it delivers:** The simplified MVP is validated as a coherent, installable Windows demo.

**Acceptance criteria:**

- [x] Onboarding and rewrites work in Notepad and one browser editor under normal network conditions within five seconds.
- [x] Consent wording, background lifetime, clipboard restoration, and safe-failure behavior are manually verified.
- [x] The GitHub Release download link is verified after a manual executable publication.

## Delivery order

`1 → 2 + 3 + 4`, then `3 → 5`, then `2 + 4 + 5 → 6`; ticket 7 can run in parallel; ticket 8 completes the MVP.

**Total estimated effort:** 13–21 engineer-days.

## Publication status

This is a local planning document. No issue tracker is configured, so no external tickets have been created.
