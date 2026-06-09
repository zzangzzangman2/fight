# Content Authoring UI

Run `run-content-authoring.cmd`, then open the printed local URL.

The editor hides raw JSON from the user. Saving converts the UI state into:

- `UnityScaffold/Assets/JoseonMurimTactics/Resources/AuthoringContent/content_manifest.json`
- uploaded media under `UnityScaffold/Assets/JoseonMurimTactics/Resources/AuthoringContent/Media`

Unity can read the saved content with `AuthoringContentManifest.LoadFromResources()`.
