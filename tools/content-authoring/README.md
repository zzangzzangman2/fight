# Content Authoring UI

Run `run-content-authoring.cmd`, then open the printed local URL.

If the local port is blocked, opening `index.html` directly still loads the
embedded default game dialogue from `defaults.js`. If `server.js` is running on
`127.0.0.1`, even a plain `file://` page can save directly into Unity Resources
through the local API instead of downloading JSON.

The editor hides raw JSON from the user. Saving converts the UI state into:

- `UnityScaffold/Assets/JoseonMurimTactics/Resources/AuthoringContent/content_manifest.json`
- uploaded media under `UnityScaffold/Assets/JoseonMurimTactics/Resources/AuthoringContent/Media`

Unity can read the saved content with `AuthoringContentManifest.LoadFromResources()`.

Direct game application requires `node server.js` because browsers cannot write
local Unity project files from a plain `file://` page.
