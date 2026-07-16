# Simplified Echo MVP — Implementation Plan

## Summary

Build a small .NET 8 Windows utility that creates one voice profile during onboarding and rewrites selected text through a managed NVIDIA NIM gateway. Include a basic download page linking to GitHub Releases.

## Desktop application

- Create a WinUI 3 app that collects 3–10 pasted samples, explains cloud processing, records consent, and displays the generated voice profile.
- Store only consent and the generated profile locally with Windows DPAPI. Do not retain editable samples or rewrite history.
- Keep Echo running as a single background process without a tray icon; launching it again brings its window forward.
- Register `Ctrl+Alt+E` to copy selected text, request a rewrite, paste the result, and restore the previous clipboard. On failure, leave text untouched and show a brief notification.

## Managed NIM processing

- Provide two gateway operations: generate a profile from onboarding samples and rewrite selected text with the saved profile.
- Call the gateway-configured NVIDIA NIM model with a server-held credential. Do not expose provider selection, BYOK settings, provider adapters, or application quotas.
- Require consent before either operation. Send only onboarding samples or selected text plus the saved profile, and do not persist customer content in gateway logs.
- Reject rewrite input above 2,000 words and instruct the model to preserve meaning, facts, language, links, and practical formatting without inventing details.

## Website and release

- Publish a static site with a product summary, privacy note, Windows requirements, and a link to the current GitHub Release.
- Publish the Windows executable manually to GitHub Releases. Defer installer signing, checksums, and release automation.

## Validation

- Test consent, sample-count validation, protected profile storage, word limits, and NIM request/response handling.
- Test clipboard success, no selection, copy timeout, model failure, unsupported target, and clipboard restoration with faked system adapters.
- Manually validate onboarding and rewriting in Notepad, then one browser editor. Target successful rewrites in under five seconds under normal network conditions.
