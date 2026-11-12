/**
 * NOVA Workspace — public site + download proxy.
 *
 * The release lives in the private GitHub repository configured as GITHUB_REPO.
 * Visitors' browsers never receive a GitHub credential: the Worker resolves the
 * latest release server-side and streams the requested package back with
 * Content-Disposition: attachment.
 *
 * Large zips may be published as byte-range parts named
 * `NAME.zip.part01`, `.part02`, … Concatenated in name order the parts are
 * byte-for-byte the original zip. Streaming uses FixedLengthStream.
 */

export default {
  async fetch(request, env) {
    const url = new URL(request.url);

    if (url.pathname === "/download") {
      if (request.method !== "GET" && request.method !== "HEAD") {
        return new Response("Method not allowed", {
          status: 405,
          headers: { Allow: "GET, HEAD" },
        });
      }
      return downloadZip(request, env, url);
    }

    return env.ASSETS.fetch(request);
  },
};

const GH_API = "https://api.github.com";
const DEFAULT_BASE = "NOVAWorkspace";
const DEFAULT_PLATFORM = "win-x64";
// platform -> release asset base name (without .zip / .partNN)
const PLATFORM_ASSETS = {
  "win-x64": "NOVAWorkspace",
  "win-x86": "NOVAWorkspace-win-x86",
  "win-arm64": "NOVAWorkspace-win-arm64",
  "macos-intel": "NOVAWorkspace-macos-intel",
  "macos-apple-silicon": "NOVAWorkspace-macos-apple-silicon",
  "ubuntu": "NOVAWorkspace-ubuntu",
};

async function downloadZip(request, env, url) {
  const repo = env.GITHUB_REPO;
  const token = env.GH_TOKEN;

  if (!repo || !token) {
    return textResponse("Download service is not configured.", 500);
  }

  const platform = (url.searchParams.get("platform") || DEFAULT_PLATFORM).toLowerCase();
  const baseName = PLATFORM_ASSETS[platform];
  if (!baseName) {
    return textResponse("Unknown download platform.", 400);
  }
  const zipName = `${baseName}.zip`;

  const releaseResp = await ghFetch(`${GH_API}/repos/${repo}/releases/latest`, token);
  if (!releaseResp.ok) {
    return textResponse(`Release lookup failed (${releaseResp.status}).`, 502);
  }

  const release = await releaseResp.json();
  const assets = release.assets || [];

  // One asset if it fits, otherwise the byte-range parts in order.
  const single = assets.find((a) => a.name === zipName);
  const parts = assets
    .filter((a) => a.name.startsWith(`${zipName}.part`))
    .sort((a, b) => a.name.localeCompare(b.name));

  const sources = single ? [single] : parts;
  if (sources.length === 0) {
    return textResponse(
      `The ${platform} download is not available yet in the latest release (${zipName}). It may require CI/toolchain build and publish.`,
      404,
    );
  }

  return streamAssets(request, sources, token, zipName, platform);
}

async function streamAssets(request, sources, token, zipName, platform) {
  const total = sources.reduce((n, a) => n + a.size, 0);

  const headers = new Headers();
  headers.set("Content-Type", "application/zip");
  headers.set("Content-Disposition", `attachment; filename="${zipName}"`);
  headers.set("Content-Length", String(total));
  headers.set("Cache-Control", "no-store");
  headers.set("X-NOVA-Parts", String(sources.length));
  headers.set("X-NOVA-Platform", platform || DEFAULT_PLATFORM);

  if (request.method === "HEAD") {
    return new Response(null, { status: 200, headers });
  }

  const { readable, writable } = new FixedLengthStream(total);

  (async () => {
    try {
      for (const asset of sources) {
        const resp = await ghFetch(asset.url, token, "application/octet-stream");
        if (!resp.ok || !resp.body) {
          throw new Error(`asset ${asset.name} -> ${resp.status}`);
        }
        await resp.body.pipeTo(writable, { preventClose: true });
      }
      await writable.close();
    } catch (err) {
      try {
        await writable.abort(err);
      } catch (_) {
        /* client already gone */
      }
    }
  })();

  return new Response(readable, { status: 200, headers });
}

function textResponse(message, status) {
  return new Response(message, {
    status,
    headers: { "Content-Type": "text/plain; charset=utf-8", "Cache-Control": "no-store" },
  });
}

function ghFetch(url, token, accept) {
  return fetch(url, {
    headers: {
      "User-Agent": "nova-workspace-download-worker",
      Authorization: `Bearer ${token}`,
      Accept: accept || "application/vnd.github+json",
    },
    redirect: "follow",
  });
}
