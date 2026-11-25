using System;
using UnityEngine;
using UnityEngine.UI;

// One entry of the list on the left (icon, number and name).
public class PokemonRow : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] Image icon;
    [SerializeField] Text numberLabel;
    [SerializeField] Text nameLabel;
    [SerializeField] Button button;
    [SerializeField] Color normalColor = new Color(0.11f, 0.11f, 0.11f);
    [SerializeField] Color selectedColor = new Color(0.86f, 0.86f, 0.86f);

    public int Id { get; private set; }
    public string PokemonName { get; private set; }

    public void Setup(int id, string pokemonName, Action<PokemonRow> onClick)
    {
        Id = id;
        PokemonName = pokemonName;
        numberLabel.text = $"#{id:000}";
        nameLabel.text = Pretty.Name(pokemonName);
        icon.enabled = false;
        button.onClick.AddListener(() => onClick(this));
        SetSelected(false);
    }

    public void SetIcon(Sprite sprite)
    {
        icon.sprite = sprite;
        icon.enabled = sprite != null;
    }

    public void SetSelected(bool selected)
    {
        background.color = selected ? selectedColor : normalColor;
        var text = selected ? new Color(0.07f, 0.07f, 0.07f) : new Color(0.9f, 0.9f, 0.9f);
        nameLabel.color = text;
        numberLabel.color = selected ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
    }
}
