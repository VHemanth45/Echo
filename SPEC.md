# Echo Windows MVP — Rewrite in My Voice

## Problem Statement

Professionals need to make draft or AI-generated text sound like them without leaving the application where they are writing. Echo should provide a fast Windows shortcut while making the cloud-data boundary clear.

## Solution

Echo is a small Windows background utility. A user completes one onboarding flow by pasting three to ten writing samples and consenting to cloud processing. Echo uses a managed NVIDIA NIM gateway to generate and locally save one voice profile. The user can then select editable text and press `Ctrl+Alt+E` to replace it with a meaning-preserving rewrite in that voice.

Echo includes a simple public download page that links visitors to the latest GitHub Release.

## User Stories

1. As a new user, I want to understand what Echo stores and sends before I continue, so that I can make an informed privacy choice.
2. As a new user, I want to paste three to ten writing samples during onboarding, so that Echo can create a reusable voice profile.
3. As a user, I want to review the generated profile, so that I understand the style Echo will apply.
4. As a privacy-conscious user, I want to explicitly consent before cloud processing occurs, so that text is never sent unexpectedly.
5. As a user, I want Echo to retain my consent and generated profile locally, so that subsequent rewrites need no setup.
6. As a writer, I want to select text and press `Ctrl+Alt+E`, so that I can rewrite it in place without switching applications.
7. As a writer, I want Echo to preserve my meaning, facts, language, links, and practical formatting, so that the rewrite remains safe to use.
8. As a writer, I want Echo not to alter my document on selection, clipboard, provider, or target failure, so that I do not lose work.
9. As a writer, I want my existing clipboard content restored after a rewrite attempt, so that Echo does not interrupt my workflow.
10. As a user, I want Echo to remain available in the background after I close its window, so that the global shortcut continues to work.
11. As a visitor, I want a basic website with product, privacy, and Windows information plus a release download link, so that I can decide whether to install Echo.
12. As an operator, I want the NVIDIA credential and model configuration to remain on the gateway, so that they are not exposed to desktop users.

## Implementation Decisions

- Replace the Python starter with a .NET 8 WinUI 3 desktop application and a minimal ASP.NET Core gateway.
- Implement only the managed NVIDIA NIM path. The gateway exposes profile generation and rewrite operations and uses its configured NIM model and server-held credential directly.
- Require one onboarding sequence of three to ten samples and explicit persisted cloud consent. Store only consent and the generated voice profile with Windows DPAPI; do not retain samples after profile generation.
- Do not include profile or sample editing, regeneration, history, tray controls, advanced settings, BYOK, provider abstraction, or application-enforced demo quotas.
- Register `Ctrl+Alt+E` while the single-instance desktop process runs in the background. Starting Echo again brings its window forward; explicitly exiting it disables the shortcut.
- Use copy, bounded clipboard read, rewrite, paste, and clipboard restoration for replacement. Failure at any stage does not paste or otherwise alter the selected text.
- Limit input to 2,000 words. Prompts require preservation of meaning, facts, language, links, and practical formatting and prohibit invented details.
- Persist no selected text, samples, profile content, or generated output in gateway logs.
- Build a static website that links to the latest GitHub Release. Publish the Windows executable manually; signing, checksums, and release automation are deferred.

## Testing Decisions

- Test observable outcomes at the rewrite-orchestration boundary using fake clipboard and gateway adapters.
- Unit-test consent gating, sample-count validation, protected profile storage, word limits, and NIM response/error normalization.
- Verify success, no selection, copy timeout, provider failure, unsupported paste target, and prior-clipboard restoration.
- Use a mocked NIM-compatible service for gateway integration tests and assert content does not enter persistent logs.
- Manually test onboarding and rewrite behavior in Notepad and one browser editor. A successful rewrite should complete within five seconds under normal network conditions.

## Out of Scope

- Operating systems other than Windows; accounts, sync, billing, analytics, browser extensions, and integrations.
- BYOK, provider choice, user-visible NVIDIA credentials, profile/sample editing or regeneration, rewrite history, and tray controls.
- Preview before replacement, custom rich-text editing, application-level usage quotas, and a fraud-prevention system.
- Installer signing, checksums, CI/CD release automation, Microsoft Store distribution, and direct CDN hosting.

## Further Notes

- Echo sends onboarding samples for profile generation and sends selected text plus the saved profile for rewrites only after consent.
- Clipboard interoperability defines supported editors. Unsupported applications result in a notification and no text change.
