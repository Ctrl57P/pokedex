using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Small helper around UnityWebRequest: downloads JSON and sprites from PokéAPI.
// Everything downloaded is cached in this component (no static state).
public class PokeApiClient : MonoBehaviour
{
    public const string ListUrl = "https://pokeapi.co/api/v2/pokemon/?offset=20&limit=100";
    const string SpriteBase = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/";

    readonly Dictionary<string, string> jsonCache = new Dictionary<string, string>();
    readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    public static string IconUrl(int id) => $"{SpriteBase}{id}.png";

    // The list only gives "name" + "url"; the id is the last number in the url.
    public static int IdFromUrl(string url)
    {
        var parts = url.TrimEnd('/').Split('/');
        return int.TryParse(parts[parts.Length - 1], out int id) ? id : 0;
    }

    public IEnumerator GetJson<T>(string url, Action<T> onDone, Action<string> onError)
    {
        if (jsonCache.TryGetValue(url, out string cached))
        {
            onDone(JsonUtility.FromJson<T>(cached));
            yield break;
        }
        using var req = UnityWebRequest.Get(url);
        req.timeout = 20;
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error);
            yield break;
        }
        jsonCache[url] = req.downloadHandler.text;
        onDone(JsonUtility.FromJson<T>(req.downloadHandler.text));
    }

    public IEnumerator GetSprite(string url, Action<Sprite> onDone)
    {
        if (string.IsNullOrEmpty(url)) { onDone(null); yield break; }
        if (spriteCache.TryGetValue(url, out Sprite cached)) { onDone(cached); yield break; }

        using var req = UnityWebRequestTexture.GetTexture(url, true);
        req.timeout = 20;
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) { onDone(null); yield break; }

        Texture2D tex = DownloadHandlerTexture.GetContent(req);
        tex.filterMode = FilterMode.Point;            // keep the pixel-art look sharp
        tex.wrapMode = TextureWrapMode.Clamp;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = url;
        spriteCache[url] = sprite;
        onDone(sprite);
    }
}
