using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Lekha.UI
{
    /// <summary>
    /// Singleton MonoBehaviour that can run web-load coroutines on behalf of static callers.
    /// Auto-creates itself when first accessed.
    /// </summary>
    public class WebResourceLoader : MonoBehaviour
    {
        public static WebResourceLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("WebResourceLoader");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<WebResourceLoader>();
                }
                return _instance;
            }
        }
        private static WebResourceLoader _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Download a texture from URL and return a Sprite via callback.
        /// Checks cache first.
        /// </summary>
        public void LoadSprite(string url, string cacheKey,
            Dictionary<string, Sprite> cache,
            Action<Sprite> onSuccess)
        {
            StartCoroutine(LoadSpriteCoroutine(url, cacheKey, cache, onSuccess));
        }

        private IEnumerator LoadSpriteCoroutine(string url, string cacheKey,
            Dictionary<string, Sprite> cache,
            Action<Sprite> onSuccess)
        {
            if (cache != null && cache.TryGetValue(cacheKey, out Sprite cached))
            {
                onSuccess?.Invoke(cached);
                yield break;
            }

            using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                if (tex != null && tex.width > 4)
                {
                    tex.filterMode = FilterMode.Bilinear;
                    Sprite sprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);

                    cache?.Add(cacheKey, sprite);
                    onSuccess?.Invoke(sprite);
                    yield break;
                }
            }

            Debug.LogWarning($"[WebResourceLoader] Failed: {url} — {req.error}");
            onSuccess?.Invoke(null);
        }
    }
}
