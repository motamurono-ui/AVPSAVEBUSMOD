using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ModSystem
{
    /// <summary>
    /// Loads AssetBundles from a mod's folder at runtime.
    /// Supports textures, audio clips, sprites, prefabs, and UI canvases.
    /// </summary>
    public static class ModAssetLoader
    {
        // Cache loaded bundles so we can unload them later
        static readonly Dictionary<string, AssetBundle> _bundles = new();

        // Load

        /// <summary>
        /// Load an AssetBundle from disk and return all assets keyed by
        /// "modId/assetName" for registration in the mod registry.
        /// </summary>
        public static async Task<Dictionary<string, Object>> LoadBundleAsync(
            string modId, string bundlePath)
        {
            var result = new Dictionary<string, Object>();

            if (!File.Exists(bundlePath))
            {
                Debug.LogWarning($"[ModAssetLoader] Bundle not found: {bundlePath}");
                return result;
            }

            // Unload previous version if reloading
            if (_bundles.TryGetValue(modId, out var old))
            {
                old.Unload(true);
                _bundles.Remove(modId);
            }

            // Load via UnityWebRequest — works in build + editor
            var tcs = new TaskCompletionSource<AssetBundle>();
            var req = UnityWebRequestAssetBundle.GetAssetBundle($"file://{bundlePath}");

            req.SendWebRequest().completed += _ =>
            {
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ModAssetLoader] Failed to load bundle '{bundlePath}': {req.error}");
                    tcs.SetResult(null);
                }
                else
                {
                    tcs.SetResult(DownloadHandlerAssetBundle.GetContent(req));
                }
            };

            var bundle = await tcs.Task;
            if (bundle == null) return result;

            _bundles[modId] = bundle;

            // Register every asset in the bundle under "modId/assetname"
            foreach (var name in bundle.GetAllAssetNames())
            {
                var asset = bundle.LoadAsset<Object>(name);
                if (asset == null) continue;

                // Use just the filename without extension as the key suffix
                string keyName = Path.GetFileNameWithoutExtension(name).ToLowerInvariant();
                string fullKey = $"{modId}/{keyName}";
                result[fullKey] = asset;
            }

            Debug.Log($"[ModAssetLoader] Loaded {result.Count} assets from mod '{modId}'.");
            return result;
        }

        /// <summary>
        /// Load a raw texture from a PNG/JPG file on disk (no AssetBundle needed).
        /// Key format: "modId/textures/filename"
        /// </summary>
        public static async Task<Dictionary<string, Object>> LoadLooseTexturesAsync(
            string modId, string texturesFolder)
        {
            var result = new Dictionary<string, Object>();
            if (!Directory.Exists(texturesFolder)) return result;

            foreach (var file in Directory.GetFiles(texturesFolder, "*.*"))
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;

                var tcs = new TaskCompletionSource<Texture2D>();
                var req = UnityWebRequestTexture.GetTexture($"file://{file}");

                req.SendWebRequest().completed += _ =>
                {
                    tcs.SetResult(req.result == UnityWebRequest.Result.Success
                        ? DownloadHandlerTexture.GetContent(req)
                        : null);
                };

                var tex = await tcs.Task;
                if (tex == null) continue;

                string keyName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                result[$"{modId}/textures/{keyName}"] = tex;
            }

            return result;
        }

        /// <summary>
        /// Load a raw AudioClip from a WAV/OGG file on disk.
        /// Key format: "modId/audio/filename"
        /// </summary>
        public static async Task<Dictionary<string, Object>> LoadLooseAudioAsync(
            string modId, string audioFolder)
        {
            var result = new Dictionary<string, Object>();
            if (!Directory.Exists(audioFolder)) return result;

            foreach (var file in Directory.GetFiles(audioFolder, "*.*"))
            {
                string ext = Path.GetExtension(file).ToLower();
                AudioType type = ext switch
                {
                    ".wav" => AudioType.WAV,
                    ".ogg" => AudioType.OGGVORBIS,
                    ".mp3" => AudioType.MPEG,
                    _ => AudioType.UNKNOWN
                };
                if (type == AudioType.UNKNOWN) continue;

                var tcs = new TaskCompletionSource<AudioClip>();
                var req = UnityWebRequestMultimedia.GetAudioClip($"file://{file}", type);

                req.SendWebRequest().completed += _ =>
                {
                    tcs.SetResult(req.result == UnityWebRequest.Result.Success
                        ? DownloadHandlerAudioClip.GetContent(req)
                        : null);
                };

                var clip = await tcs.Task;
                if (clip == null) continue;

                string keyName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                result[$"{modId}/audio/{keyName}"] = clip;
            }

            return result;
        }

        // Unload

        public static void UnloadBundle(string modId)
        {
            if (!_bundles.TryGetValue(modId, out var bundle)) return;
            bundle.Unload(true);
            _bundles.Remove(modId);
            Debug.Log($"[ModAssetLoader] Unloaded bundle for mod '{modId}'.");
        }

        public static void UnloadAll()
        {
            foreach (var b in _bundles.Values) b.Unload(true);
            _bundles.Clear();
        }
    }
}