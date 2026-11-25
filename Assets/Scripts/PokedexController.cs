using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Downloads the list (offset=20, limit=100), fills the scroll list,
// filters it with the search box and sends the selection to the DetailView.
[RequireComponent(typeof(PokeApiClient))]
public class PokedexController : MonoBehaviour
{
    [SerializeField] PokemonRow rowPrefab;
    [SerializeField] RectTransform listContent;
    [SerializeField] ScrollRect listScroll;
    [SerializeField] InputField searchField;
    [SerializeField] Text countLabel;
    [SerializeField] Text listStatus;
    [SerializeField] Button retryButton;
    [SerializeField] DetailView detail;

    PokeApiClient api;
    readonly List<PokemonRow> rows = new List<PokemonRow>();
    PokemonRow selected;

    void Start()
    {
        api = GetComponent<PokeApiClient>();
        searchField.onValueChanged.AddListener(ApplyFilter);
        retryButton.onClick.AddListener(LoadList);
        LoadList();
    }

    void LoadList()
    {
        retryButton.gameObject.SetActive(false);
        listStatus.gameObject.SetActive(true);
        listStatus.text = "Loading Pokédex…";
        StartCoroutine(api.GetJson<PokemonPage>(PokeApiClient.ListUrl, OnListLoaded, error =>
        {
            listStatus.text = $"Could not reach PokéAPI\n{error}";
            retryButton.gameObject.SetActive(true);
        }));
    }

    void OnListLoaded(PokemonPage page)
    {
        listStatus.gameObject.SetActive(false);
        foreach (var r in rows) Destroy(r.gameObject);
        rows.Clear();

        foreach (var entry in page.results)
        {
            int id = PokeApiClient.IdFromUrl(entry.url);
            PokemonRow row = Instantiate(rowPrefab, listContent);
            row.name = $"Row {id:000} {entry.name}";
            row.Setup(id, entry.name, Select);
            rows.Add(row);
            StartCoroutine(api.GetSprite(PokeApiClient.IconUrl(id), row.SetIcon));
        }
        ApplyFilter(searchField.text);
        if (rows.Count > 0) Select(rows[0]);
    }

    void Select(PokemonRow row)
    {
        if (selected != null) selected.SetSelected(false);
        selected = row;
        row.SetSelected(true);
        detail.Load(row.Id, row.PokemonName);
    }

    // search by name ("pika") or number ("25" / "#025")
    void ApplyFilter(string query)
    {
        query = query.Trim().TrimStart('#').ToLowerInvariant();
        int shown = 0;
        foreach (var row in rows)
        {
            bool match = query.Length == 0
                         || row.PokemonName.Contains(query)
                         || row.Id.ToString("000").Contains(query)
                         || row.Id.ToString() == query;
            row.gameObject.SetActive(match);
            if (match) shown++;
        }
        countLabel.text = shown == rows.Count ? $"{rows.Count} Pokémon" : $"{shown} of {rows.Count}";
        listStatus.gameObject.SetActive(rows.Count > 0 && shown == 0);
        if (rows.Count > 0 && shown == 0) listStatus.text = "No Pokémon match your search";
    }

    void Update()
    {
        // arrow keys move through the visible list (when not typing in the search box)
        var kb = Keyboard.current;
        if (kb == null || rows.Count == 0 || searchField.isFocused) return;
        int dir = kb.downArrowKey.wasPressedThisFrame ? 1 : kb.upArrowKey.wasPressedThisFrame ? -1 : 0;
        if (dir == 0) return;

        int index = selected != null ? rows.IndexOf(selected) : -1;
        for (int i = index + dir; i >= 0 && i < rows.Count; i += dir)
        {
            if (!rows[i].gameObject.activeSelf) continue;
            Select(rows[i]);
            KeepVisible(rows[i].transform as RectTransform);
            break;
        }
    }

    void KeepVisible(RectTransform item)
    {
        Canvas.ForceUpdateCanvases();
        var viewport = listScroll.viewport;
        float contentHeight = listContent.rect.height;
        float viewHeight = viewport.rect.height;
        if (contentHeight <= viewHeight) return;
        float itemTop = -item.anchoredPosition.y - item.rect.height * item.pivot.y;
        float itemBottom = itemTop + item.rect.height;
        float scrollTop = (1f - listScroll.verticalNormalizedPosition) * (contentHeight - viewHeight);
        if (itemTop < scrollTop)
            listScroll.verticalNormalizedPosition = 1f - itemTop / (contentHeight - viewHeight);
        else if (itemBottom > scrollTop + viewHeight)
            listScroll.verticalNormalizedPosition = 1f - (itemBottom - viewHeight) / (contentHeight - viewHeight);
    }
}
