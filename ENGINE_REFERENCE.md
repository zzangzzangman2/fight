# Engine Reference: Random Chat

Reference game:

- Steam app: `2244200`
- Title: `Random Chat` / `랜덤채팅의 그녀`
- Developer: TALESSHOP Co., Ltd.
- Publisher: TALESSHOP Co., Ltd., Smilegate
- Steam: `https://store.steampowered.com/app/2244200/Random_Chat/`
- SteamDB app: `https://steamdb.info/app/2244200/info/`
- SteamDB depot: `https://steamdb.info/depot/2244201/`

## Confirmed Engine

The reference game uses Unity.

Evidence from public SteamDB data:

- `Technologies: Unity Engine`
- Detected technologies include `Unity Engine`, `UnityBurst SDK`, and `UnityURP SDK`.
- Depot file list includes `RandomChat_Data`, `UnityPlayer.dll`, `UnityEngine.*` modules, `Unity.RenderPipelines.Universal.*`, `Unity.2D.Animation.Runtime.dll`, `Unity.2D.IK.Runtime.dll`, `Unity.2D.SpriteShape.Runtime.dll`, `UnityEngine.TilemapModule.dll`, and `Unity.Timeline.dll`.
- This is enough to treat Unity as confirmed, not just visually guessed from the MP4.

## Detected Unity Stack

Use this as the closest implementation direction:

```text
Unity 2D + URP
2D Tilemap/Grid for map layout
Unity 2D Animation / 2D IK or Spine for character sprites
SpriteShape or Tilemap layers for roads, school grounds, stairs, paths, river edges
Timeline for event/cutscene cues
DOTween-style tweening for movement, camera, UI, speech bubbles
TextMeshPro for Korean UI text
A* Pathfinding style service for navigation
Rewired-style input abstraction if controller support matters
```

## Visual Ratio Notes

`C:\Users\sjpark\Downloads\2d_map.mp4` is a 640x360, 30fps, 16:9 capture.

Observed visual proportions:

- Quarter-view/isometric-like 2D school map.
- Characters are larger than old SRPG pawn tokens.
- Character height is roughly one tile high, with large head and simple body proportions.
- Best prototype ratio: one tile = one character height envelope, character body overlaps slightly above its tile.

## Project Rule

Match the engine and proportions, not the original copyrighted art.

The prototype should use original Joseon/murim characters and maps while borrowing only the implementation approach:

- Unity 2D URP
- Tilemap/Grid gameplay
- Sprite-based people
- Smooth tweened movement
- Dialogue/interaction overlays
- 16:9 camera framing
