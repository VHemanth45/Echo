# Echo MVP

Echo is a Windows background utility that generates an encrypted local voice profile and rewrites selected text with `Ctrl+Alt+E` through a managed NVIDIA NIM gateway.

## Run locally

1. Start the gateway: `dotnet run --project src/Echo.Gateway --urls http://localhost:5000`.
2. Run the desktop app: `dotnet run --project src/Echo.Desktop`.
3. Paste 3–10 samples separated by `---`, consent, and create a profile. The unconfigured gateway uses a safe local demo response; configure `Nim:Endpoint`, `Nim:ApiKey`, and `Nim:Model` via environment/user secrets for NVIDIA NIM.

## Verification and publication

Run `dotnet run --project tests/Echo.Tests`. Publish manually with `dotnet publish src/Echo.Desktop -c Release -r win-x64 --self-contained true -o publish`, then upload `publish` to GitHub Releases. The static download page is in `site/` and is GitHub Pages-ready.

The gateway never logs customer content. It validates consent, sample count, and the 2,000-word rewrite limit. The desktop app preserves the previous clipboard object after each attempt and only pastes following a successful rewrite.
