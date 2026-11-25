using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Right side of the Pokédex: artwork, name, types, measures, description and stats.
public class DetailView : MonoBehaviour
{
    [Header("Artwork")]
    [SerializeField] Image artwork;
    [SerializeField] Button artworkButton;
    [SerializeField] Text viewLabel;

    [Header("Info")]
    [SerializeField] Text numberLabel;
    [SerializeField] Text nameLabel;
    [SerializeField] Text genusLabel;
    [SerializeField] Transform typesRow;
    [SerializeField] GameObject typeChipTemplate;
    [SerializeField] Text heightLabel;
    [SerializeField] Text weightLabel;
    [SerializeField] Text abilitiesLabel;
    [SerializeField] Text flavorLabel;

    [Header("Stats")]
    [SerializeField] StatBar[] statBars;
    [SerializeField] Text totalLabel;

    [Header("State")]
    [SerializeField] CanvasGroup content;
    [SerializeField] Text statusLabel;

    static readonly string[] ViewNames = { "FRONT", "BACK", "SHINY", "SHINY BACK" };

    PokeApiClient api;
    PokemonDetail current;
    int viewIndex;
    int requestedId;
    readonly List<GameObject> chips = new List<GameObject>();

    void Awake()
    {
        api = GetComponentInParent<PokeApiClient>();
        artworkButton.onClick.AddListener(NextView);
        typeChipTemplate.SetActive(false);
        ShowStatus("Select a Pokémon from the list");
    }

    public void ShowStatus(string message)
    {
        statusLabel.text = message;
        statusLabel.gameObject.SetActive(true);
        content.alpha = 0f;
    }

    public void Load(int id, string pokemonName)
    {
        requestedId = id;
        // show what we already know while the details download
        numberLabel.text = $"#{id:000}";
        nameLabel.text = Pretty.Name(pokemonName);
        content.alpha = 1f;
        statusLabel.gameObject.SetActive(false);
        genusLabel.text = "";
        flavorLabel.text = "Loading…";
        // clear the previous Pokémon so its data is never shown under the new name
        current = null;
        artwork.enabled = false;
        viewLabel.text = "";
        foreach (var c in chips) Destroy(c);
        chips.Clear();
        heightLabel.text = "-";
        weightLabel.text = "-";
        abilitiesLabel.text = "";
        foreach (var bar in statBars) bar.Clear();
        totalLabel.text = "";

        StartCoroutine(api.GetJson<PokemonDetail>($"https://pokeapi.co/api/v2/pokemon/{id}",
            detail => { if (detail.id == requestedId) ShowDetail(detail); },
            error => { if (id == requestedId) flavorLabel.text = $"Could not load this Pokémon ({error})."; }));

        StartCoroutine(api.GetJson<PokemonSpecies>($"https://pokeapi.co/api/v2/pokemon-species/{id}",
            species => { if (id == requestedId) ShowSpecies(species); },
            _ => { }));
    }

    void ShowDetail(PokemonDetail d)
    {
        current = d;
        viewIndex = 0;
        ShowArtwork();

        foreach (var c in chips) Destroy(c);
        chips.Clear();
        foreach (var t in d.types.OrderBy(t => t.slot))
        {
            var chip = Instantiate(typeChipTemplate, typesRow);
            chip.GetComponentInChildren<Text>().text = t.type.name.ToUpperInvariant();
            chip.SetActive(true);
            chips.Add(chip);
        }

        heightLabel.text = $"{d.height / 10f:0.0} m";
        weightLabel.text = $"{d.weight / 10f:0.0} kg";
        abilitiesLabel.text = string.Join(", ",
            d.abilities.Select(a => Pretty.Name(a.ability.name) + (a.is_hidden ? " (hidden)" : "")));

        int total = 0;
        for (int i = 0; i < statBars.Length; i++)
        {
            if (i < d.stats.Length)
            {
                statBars[i].Show(Pretty.Stat(d.stats[i].stat.name), d.stats[i].base_stat);
                total += d.stats[i].base_stat;
            }
            else statBars[i].Clear();
        }
        totalLabel.text = $"Total  {total}";
    }

    void ShowSpecies(PokemonSpecies s)
    {
        var genus = s.genera?.FirstOrDefault(g => g.language.name == "en");
        genusLabel.text = genus != null ? genus.genus : "";
        // the newest English entry is usually the last one
        var text = s.flavor_text_entries?.LastOrDefault(f => f.language.name == "en");
        flavorLabel.text = text != null ? Pretty.Flavor(text.flavor_text) : "No description available.";
    }

    string UrlForView(int index)
    {
        switch (index)
        {
            case 1: return current.sprites.back_default;
            case 2: return current.sprites.front_shiny;
            case 3: return current.sprites.back_shiny;
            default: return current.sprites.front_default;
        }
    }

    // clicking the artwork cycles front -> back -> shiny -> shiny back
    void NextView()
    {
        if (current == null) return;
        for (int step = 1; step <= ViewNames.Length; step++)
        {
            int next = (viewIndex + step) % ViewNames.Length;
            if (!string.IsNullOrEmpty(UrlForView(next))) { viewIndex = next; break; }
        }
        ShowArtwork();
    }

    void ShowArtwork()
    {
        int id = current.id;
        viewLabel.text = $"{ViewNames[viewIndex]}  ·  click to rotate";
        StartCoroutine(api.GetSprite(UrlForView(viewIndex), sprite =>
        {
            if (id != requestedId) return;
            artwork.sprite = sprite;
            artwork.enabled = sprite != null;
        }));
    }
}
