using System;

// Plain C# classes that mirror the parts of the PokéAPI JSON we use.
// JsonUtility ignores every field that is not declared here.

[Serializable] public class NamedResource { public string name; public string url; }

// GET https://pokeapi.co/api/v2/pokemon/?offset=20&limit=100
[Serializable] public class PokemonPage
{
    public int count;
    public string next;
    public string previous;
    public NamedResource[] results;
}

// GET https://pokeapi.co/api/v2/pokemon/{id}
[Serializable] public class PokemonDetail
{
    public int id;
    public string name;
    public int height;   // decimetres
    public int weight;   // hectograms
    public int base_experience;
    public TypeSlot[] types;
    public StatEntry[] stats;
    public AbilitySlot[] abilities;
    public PokemonSprites sprites;
}

[Serializable] public class TypeSlot { public int slot; public NamedResource type; }
[Serializable] public class StatEntry { public int base_stat; public NamedResource stat; }
[Serializable] public class AbilitySlot { public bool is_hidden; public NamedResource ability; }
[Serializable] public class PokemonSprites
{
    public string front_default;
    public string back_default;
    public string front_shiny;
    public string back_shiny;
}

// GET https://pokeapi.co/api/v2/pokemon-species/{id}  (Pokédex description + category)
[Serializable] public class PokemonSpecies
{
    public FlavorText[] flavor_text_entries;
    public Genus[] genera;
}
[Serializable] public class FlavorText { public string flavor_text; public NamedResource language; public NamedResource version; }
[Serializable] public class Genus { public string genus; public NamedResource language; }
