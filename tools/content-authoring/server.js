const fs = require("fs");
const http = require("http");
const path = require("path");
const { URL } = require("url");

const repoRoot = path.resolve(__dirname, "..", "..");
const publicRoot = __dirname;
const resourcesRoot = path.join(repoRoot, "UnityScaffold", "Assets", "JoseonMurimTactics", "Resources");
const outputRoot = path.join(resourcesRoot, "AuthoringContent");
const mediaRoot = path.join(outputRoot, "Media");
const manifestPath = path.join(outputRoot, "content_manifest.json");
const backupRoot = path.join(outputRoot, "Backups");
const port = Number(process.env.PORT || 5178);

const mediaFolders = {
  backgrounds: "Backgrounds",
  portraits: "Portraits",
  props: "Props"
};

const mimeTypes = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "application/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".webp": "image/webp",
  ".gif": "image/gif",
  ".mp3": "audio/mpeg",
  ".wav": "audio/wav",
  ".ogg": "audio/ogg"
};

function ensureDirs() {
  fs.mkdirSync(outputRoot, { recursive: true });
  fs.mkdirSync(backupRoot, { recursive: true });
  for (const folder of Object.values(mediaFolders)) {
    fs.mkdirSync(path.join(mediaRoot, folder), { recursive: true });
  }
}

function send(res, status, body, type = "application/json; charset=utf-8") {
  const payload = Buffer.isBuffer(body) ? body : Buffer.from(body);
  res.writeHead(status, {
    "Content-Type": type,
    "Content-Length": payload.length
  });
  res.end(payload);
}

function sendJson(res, status, data) {
  send(res, status, JSON.stringify(data), "application/json; charset=utf-8");
}

function readBody(req, limitBytes = 80 * 1024 * 1024) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    req.on("data", chunk => {
      size += chunk.length;
      if (size > limitBytes) {
        reject(new Error("payload too large"));
        req.destroy();
        return;
      }

      chunks.push(chunk);
    });
    req.on("end", () => resolve(Buffer.concat(chunks)));
    req.on("error", reject);
  });
}

function safeJoin(root, requestPath) {
  const target = path.resolve(root, requestPath.replace(/^[/\\]+/, ""));
  if (!target.startsWith(path.resolve(root))) {
    return null;
  }

  return target;
}

function slug(value, fallback = "asset") {
  const ascii = String(value || "")
    .normalize("NFKD")
    .replace(/[^\w.-]+/g, "_")
    .replace(/^_+|_+$/g, "")
    .toLowerCase();

  return ascii || `${fallback}_${Date.now()}`;
}

function defaultManifest() {
  return {
    version: 1,
    updatedAt: new Date().toISOString(),
    project: {
      title: "조선 무협 SRPG",
      note: "Unity를 열지 않고 작성한 대사/배경/에셋 매니페스트"
    },
    characters: [
      {
        id: "park_sungjun",
        displayName: "박성준",
        role: "해동문 문주",
        portraitId: "",
        portraitResource: "",
        notes: ""
      },
      {
        id: "yun_seohwa",
        displayName: "윤서화",
        role: "예검 반격수",
        portraitId: "",
        portraitResource: "",
        notes: ""
      }
    ],
    backgrounds: [
      {
        id: "jido_1",
        title: "전략 지도 1",
        resourcePath: "WorldMap/jido_1",
        previewUrl: "/resources/WorldMap/jido_1.png",
        notes: ""
      }
    ],
    portraits: [],
    props: [],
    dialogueScenes: [
      {
        id: "prologue_placeholder",
        title: "대사 작성 준비",
        location: "폐사당",
        backgroundId: "jido_1",
        startNodeId: "prologue_placeholder_001",
        entries: [
          {
            id: "line_001",
            speakerId: "park_sungjun",
            line: "여기에 요청하실 대사를 넣을 준비가 되어 있습니다.",
            mood: "보통",
            backgroundId: "jido_1",
            choices: []
          }
        ],
        nodes: []
      }
    ]
  };
}

function loadManifest() {
  ensureDirs();
  if (!fs.existsSync(manifestPath)) {
    return defaultManifest();
  }

  try {
    return JSON.parse(fs.readFileSync(manifestPath, "utf8"));
  } catch (error) {
    const fallback = defaultManifest();
    fallback.project.note = `기존 매니페스트를 읽지 못했습니다: ${error.message}`;
    return fallback;
  }
}

function backupManifest() {
  if (!fs.existsSync(manifestPath)) {
    return;
  }

  const stamp = new Date().toISOString().replace(/[:.]/g, "-");
  fs.copyFileSync(manifestPath, path.join(backupRoot, `content_manifest_${stamp}.json`));
}

async function handleApi(req, res, url) {
  if (req.method === "GET" && url.pathname === "/api/content") {
    sendJson(res, 200, loadManifest());
    return true;
  }

  if (req.method === "POST" && url.pathname === "/api/content") {
    ensureDirs();
    const body = await readBody(req);
    const content = JSON.parse(body.toString("utf8"));
    content.updatedAt = new Date().toISOString();
    backupManifest();
    fs.writeFileSync(manifestPath, `${JSON.stringify(content, null, 2)}\n`, "utf8");
    sendJson(res, 200, {
      ok: true,
      manifestPath: path.relative(repoRoot, manifestPath).replace(/\\/g, "/"),
      updatedAt: content.updatedAt
    });
    return true;
  }

  if (req.method === "POST" && url.pathname === "/api/upload") {
    ensureDirs();
    const kind = url.searchParams.get("kind") || "props";
    const folder = mediaFolders[kind];
    if (!folder) {
      sendJson(res, 400, { ok: false, error: "unknown media kind" });
      return true;
    }

    const body = JSON.parse((await readBody(req)).toString("utf8"));
    const originalName = body.name || "asset";
    const ext = path.extname(originalName).toLowerCase() || ".png";
    const baseName = slug(path.basename(originalName, ext), kind.slice(0, -1));
    const fileName = `${baseName}${ext}`;
    const targetPath = path.join(mediaRoot, folder, fileName);
    const comma = String(body.dataUrl || "").indexOf(",");
    const encoded = comma >= 0 ? body.dataUrl.slice(comma + 1) : body.dataUrl;
    fs.writeFileSync(targetPath, Buffer.from(encoded, "base64"));

    const resourcePath = `AuthoringContent/Media/${folder}/${baseName}`;
    sendJson(res, 200, {
      ok: true,
      id: baseName,
      title: path.basename(originalName, ext),
      fileName,
      resourcePath,
      previewUrl: `/media/${folder}/${fileName}`
    });
    return true;
  }

  return false;
}

function serveFile(res, filePath) {
  if (!filePath || !fs.existsSync(filePath) || !fs.statSync(filePath).isFile()) {
    send(res, 404, "Not found", "text/plain; charset=utf-8");
    return;
  }

  const ext = path.extname(filePath).toLowerCase();
  send(res, 200, fs.readFileSync(filePath), mimeTypes[ext] || "application/octet-stream");
}

const server = http.createServer(async (req, res) => {
  try {
    const url = new URL(req.url, `http://${req.headers.host}`);
    if (await handleApi(req, res, url)) {
      return;
    }

    if (req.method !== "GET") {
      send(res, 405, "Method not allowed", "text/plain; charset=utf-8");
      return;
    }

    if (url.pathname.startsWith("/media/")) {
      serveFile(res, safeJoin(mediaRoot, decodeURIComponent(url.pathname.slice("/media/".length))));
      return;
    }

    if (url.pathname.startsWith("/resources/")) {
      serveFile(res, safeJoin(resourcesRoot, decodeURIComponent(url.pathname.slice("/resources/".length))));
      return;
    }

    const requestPath = url.pathname === "/" ? "index.html" : decodeURIComponent(url.pathname.slice(1));
    serveFile(res, safeJoin(publicRoot, requestPath));
  } catch (error) {
    sendJson(res, 500, { ok: false, error: error.message });
  }
});

ensureDirs();
server.listen(port, "127.0.0.1", () => {
  console.log(`Content authoring UI: http://127.0.0.1:${port}`);
  console.log(`Saving to: ${manifestPath}`);
});
