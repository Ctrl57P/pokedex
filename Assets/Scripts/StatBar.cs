using UnityEngine;
using UnityEngine.UI;

// One base-stat row: "Attack  [=======     ]  90"
public class StatBar : MonoBehaviour
{
    public const float MaxStat = 200f;   // most stats are below 200, so the bars stay readable

    [SerializeField] Text label;
    [SerializeField] RectTransform fill;
    [SerializeField] Text value;

    float target, current;

    public void Show(string statName, int amount)
    {
        label.text = statName;
        value.text = amount.ToString();
        target = Mathf.Clamp01(amount / MaxStat);
    }

    public void Clear()
    {
        value.text = "-";
        target = 0f;
    }

    void Update()
    {
        // small animation so the bars grow when a new Pokémon is selected
        current = Mathf.MoveTowards(current, target, Time.deltaTime * 2.5f);
        fill.anchorMax = new Vector2(current, 1f);
    }
}
