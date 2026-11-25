using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Menu: Tools > Build Pokédex scene   -> Assets/Scenes/Pokedex.unity + Assets/Prefabs/PokemonRow.prefab
// Menu: Tools > Build WebGL           -> Builds/WebGL (the folder that is published to GitHub Pages)
public static class SceneBuilder
{
    const string ScenePath = "Assets/Scenes/Pokedex.unity";
    const string RowPrefabPath = "Assets/Prefabs/PokemonRow.prefab";

    // monochrome palette (only the Pokémon sprites have colour)
    static readonly Color Bg = Hex("0e0e0e");
    static readonly Color Panel = Hex("171717");
    static readonly Color Screen = Hex("202020");
    static readonly Color Line = Hex("2c2c2c");
    static readonly Color Ink = Hex("e6e6e6");
    static readonly Color Muted = Hex("7c7c7c");
    static readonly Color Light = Hex("dcdcdc");
    static readonly Color Dark = Hex("121212");

    static Font UIFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    static Sprite RoundRect => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    static Sprite Knob => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

    [MenuItem("Tools/Build Pokédex scene")]
    public static void Build()
    {
        foreach (var dir in new[] { "Scenes", "Prefabs" })
            if (!AssetDatabase.IsValidFolder($"Assets/{dir}")) AssetDatabase.CreateFolder("Assets", dir);

        var rowPrefab = MakeRowPrefab();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Bg;
        cam.orthographic = true;
        camGo.transform.position = new Vector3(0, 0, -10);

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        // ---------------------------------------------------------------- canvas
        var canvasGo = new GameObject("Pokedex", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<PokeApiClient>();
        var controller = canvasGo.AddComponent<PokedexController>();
        var root = canvasGo.transform;

        Img(Rect("Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Bg);

        // ---------------------------------------------------------------- header
        var header = Rect("Header", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -88), new Vector2(-24, -12));
        var lens = Rect("Lens", header, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, -24), new Vector2(48, 24));
        Img(lens, Light, Knob);
        Img(Rect("LensInner", lens, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10)), Dark, Knob);
        Txt(Rect("Title", header, new Vector2(0, 0.5f), new Vector2(0.5f, 1), new Vector2(64, 0), new Vector2(0, 0)),
            "POKÉDEX", 34, Ink, TextAnchor.LowerLeft, FontStyle.Bold);
        Txt(Rect("Subtitle", header, new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(66, 0), new Vector2(0, -2)),
            "#021 – #120  ·  data from PokéAPI", 15, Muted, TextAnchor.UpperLeft);
        var search = MakeSearchField(header);

        // ---------------------------------------------------------------- list (left)
        var listPanel = Rect("ListPanel", root, new Vector2(0, 0), new Vector2(0.36f, 1), new Vector2(24, 24), new Vector2(-10, -100));
        Img(listPanel, Panel, RoundRect);
        var countLabel = Txt(Rect("Count", listPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -40), new Vector2(-18, -8)),
            "", 15, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        Img(Rect("Divider", listPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -45), new Vector2(-12, -44)), Line);

        var scrollGo = Rect("Scroll", listPanel, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -50));
        var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
        var viewport = Rect("Viewport", scrollGo, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-12, 0));
        Img(viewport, new Color(0, 0, 0, 0));
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect("Content", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1);
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var barRt = Rect("Scrollbar", scrollGo, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-6, 0), Vector2.zero);
        Img(barRt, Line, RoundRect);
        var slide = Rect("Sliding Area", barRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var handle = Rect("Handle", slide, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var handleImg = Img(handle, Muted, RoundRect);
        var scrollbar = barRt.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImg;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        var listStatus = Txt(Rect("ListStatus", listPanel, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(20, -10), new Vector2(-20, 60)),
            "Loading Pokédex…", 18, Muted, TextAnchor.MiddleCenter);
        var retry = MakeButton(listPanel, "Retry", new Vector2(0.5f, 0.5f), new Vector2(-60, -60), new Vector2(60, -20));

        // ---------------------------------------------------------------- detail (right)
        var detailPanel = Rect("DetailPanel", root, new Vector2(0.36f, 0), new Vector2(1, 1), new Vector2(10, 24), new Vector2(-24, -100));
        Img(detailPanel, Panel, RoundRect);
        var detail = detailPanel.gameObject.AddComponent<DetailView>();
        var detailStatus = Txt(Rect("DetailStatus", detailPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
            "", 20, Muted, TextAnchor.MiddleCenter);
        var group = Rect("Content", detailPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var canvasGroup = group.gameObject.AddComponent<CanvasGroup>();

        // artwork "screen"
        var screen = Rect("Screen", group, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -304), new Vector2(304, -24));
        Img(screen, Screen, RoundRect);
        var artBtn = screen.gameObject.AddComponent<Button>();
        artBtn.transition = Selectable.Transition.ColorTint;
        artBtn.targetGraphic = screen.GetComponent<Image>();
        var artwork = Img(Rect("Artwork", screen, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12)), Color.white);
        artwork.preserveAspect = true;
        artwork.raycastTarget = false;
        var viewLabel = Txt(Rect("ViewLabel", group, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -330), new Vector2(304, -308)),
            "FRONT", 13, Muted, TextAnchor.MiddleCenter);

        // info column
        float x0 = 330;
        var number = Txt(InfoRow(group, x0, 24, 26, "Number"), "#000", 20, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        var nameT = Txt(InfoRow(group, x0, 50, 50, "Name"), "", 42, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        var genus = Txt(InfoRow(group, x0, 100, 24, "Genus"), "", 17, Muted, TextAnchor.MiddleLeft, FontStyle.Italic);

        var types = InfoRow(group, x0, 134, 30, "Types");
        var hlg = types.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8; hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        var chip = Rect("TypeChip", types, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(chip, Light, RoundRect);
        chip.gameObject.AddComponent<LayoutElement>().preferredWidth = 104;
        Txt(Rect("Label", chip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "TYPE", 14, Dark, TextAnchor.MiddleCenter, FontStyle.Bold);

        var heightV = Measure(group, x0, 184, "HEIGHT");
        var weightV = Measure(group, x0 + 160, 184, "WEIGHT");
        Txt(InfoRow(group, x0, 244, 18, "AbilitiesTitle"), "ABILITIES", 13, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        var abilities = Txt(InfoRow(group, x0, 264, 44, "Abilities"), "", 18, Ink, TextAnchor.UpperLeft);

        Img(Rect("Divider", group, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -347), new Vector2(-24, -346)), Line);

        // Pokédex entry (bottom left)
        Txt(Rect("EntryTitle", group, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -380), new Vector2(304, -360)),
            "POKÉDEX ENTRY", 13, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        var flavor = Txt(Rect("Entry", group, new Vector2(0, 0), new Vector2(0, 1), new Vector2(24, 20), new Vector2(304, -386)),
            "", 16, Ink, TextAnchor.UpperLeft);

        // base stats (bottom right)
        Txt(Rect("StatsTitle", group, new Vector2(0, 1), new Vector2(1, 1), new Vector2(x0, -380), new Vector2(-24, -360)),
            "BASE STATS", 13, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        var bars = new StatBar[6];
        string[] statNames = { "HP", "Attack", "Defense", "Sp. Atk", "Sp. Def", "Speed" };
        for (int i = 0; i < 6; i++)
            bars[i] = MakeStatBar(group, x0, 388 + i * 26, statNames[i]);
        var total = Txt(Rect("Total", group, new Vector2(0, 1), new Vector2(1, 1), new Vector2(x0, -570), new Vector2(-24, -548)),
            "Total", 15, Muted, TextAnchor.MiddleRight, FontStyle.Bold);

        // ---------------------------------------------------------------- wiring
        Wire(detail, "artwork", artwork);
        Wire(detail, "artworkButton", artBtn);
        Wire(detail, "viewLabel", viewLabel);
        Wire(detail, "numberLabel", number);
        Wire(detail, "nameLabel", nameT);
        Wire(detail, "genusLabel", genus);
        Wire(detail, "typesRow", types);
        Wire(detail, "typeChipTemplate", chip.gameObject);
        Wire(detail, "heightLabel", heightV);
        Wire(detail, "weightLabel", weightV);
        Wire(detail, "abilitiesLabel", abilities);
        Wire(detail, "flavorLabel", flavor);
        WireArray(detail, "statBars", bars);
        Wire(detail, "totalLabel", total);
        Wire(detail, "content", canvasGroup);
        Wire(detail, "statusLabel", detailStatus);

        Wire(controller, "rowPrefab", rowPrefab);
        Wire(controller, "listContent", content);
        Wire(controller, "listScroll", scroll);
        Wire(controller, "searchField", search);
        Wire(controller, "countLabel", countLabel);
        Wire(controller, "listStatus", listStatus);
        Wire(controller, "retryButton", retry);
        Wire(controller, "detail", detail);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        ConfigurePlayer();
        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilder] Pokédex scene built");
    }

    [MenuItem("Tools/Build WebGL")]
    public static void BuildWebGL()
    {
        ConfigurePlayer();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });
        Debug.Log($"[SceneBuilder] WebGL {report.summary.result}  size={report.summary.totalSize / 1024} KB  time={report.summary.totalTime}");
        if (report.summary.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
    }

    static void ConfigurePlayer()
    {
        PlayerSettings.companyName = "School";
        PlayerSettings.productName = "Pokedex";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        // GitHub Pages cannot send Content-Encoding headers, so ship the build uncompressed
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
    }

    // ------------------------------------------------------------------ pieces

    static GameObject MakeRowPrefab()
    {
        var go = new GameObject("PokemonRow", typeof(RectTransform));
        go.AddComponent<LayoutElement>().preferredHeight = 56;
        var bg = Img(go.GetComponent<RectTransform>(), Panel, RoundRect);
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        var icon = Img(Rect("Icon", go.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(4, -28), new Vector2(60, 28)), Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var num = Txt(Rect("Number", go.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(68, 0), new Vector2(124, 0)),
            "#000", 16, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        var nm = Txt(Rect("Name", go.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(128, 0), new Vector2(-8, 0)),
            "Name", 20, Ink, TextAnchor.MiddleLeft);
        var row = go.AddComponent<PokemonRow>();
        Wire(row, "background", bg);
        Wire(row, "icon", icon);
        Wire(row, "numberLabel", num);
        Wire(row, "nameLabel", nm);
        Wire(row, "button", btn);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, RowPrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static InputField MakeSearchField(Transform header)
    {
        var rt = Rect("Search", header, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-340, -22), new Vector2(0, 22));
        var bg = Img(rt, Panel, RoundRect);
        Img(Rect("Icon", rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, -8), new Vector2(30, 8)), Muted, Knob);
        Img(Rect("IconHole", rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(17, -5), new Vector2(27, 5)), Panel, Knob);
        var placeholder = Txt(Rect("Placeholder", rt, Vector2.zero, Vector2.one, new Vector2(40, 0), new Vector2(-12, 0)),
            "Search by name or number…", 17, Muted, TextAnchor.MiddleLeft, FontStyle.Italic);
        var text = Txt(Rect("Text", rt, Vector2.zero, Vector2.one, new Vector2(40, 0), new Vector2(-12, 0)),
            "", 17, Ink, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        var field = rt.gameObject.AddComponent<InputField>();
        field.targetGraphic = bg;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.caretColor = Ink;
        field.selectionColor = new Color(1, 1, 1, 0.25f);
        field.characterLimit = 20;
        return field;
    }

    static Button MakeButton(Transform parent, string label, Vector2 anchor, Vector2 min, Vector2 max)
    {
        var rt = Rect(label + "Button", parent, anchor, anchor, min, max);
        var img = Img(rt, Light, RoundRect);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        Txt(Rect("Label", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), label.ToUpperInvariant(), 15, Dark, TextAnchor.MiddleCenter, FontStyle.Bold);
        rt.gameObject.SetActive(false);
        return b;
    }

    static Text Measure(Transform parent, float x, float y, string title)
    {
        Txt(Rect(title + "Title", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - 18), new Vector2(x + 150, -y)),
            title, 13, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        return Txt(Rect(title + "Value", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - 48), new Vector2(x + 150, -y - 20)),
            "-", 24, Ink, TextAnchor.MiddleLeft);
    }

    static StatBar MakeStatBar(Transform parent, float x, float y, string statName)
    {
        var rt = Rect("Stat " + statName, parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(x, -y - 22), new Vector2(-24, -y));
        var label = Txt(Rect("Label", rt, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(80, 0)),
            statName, 15, Muted, TextAnchor.MiddleLeft);
        var track = Rect("Track", rt, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(86, -5), new Vector2(-50, 5));
        Img(track, Line, RoundRect);
        var fill = Rect("Fill", track, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
        Img(fill, Light, RoundRect);
        var value = Txt(Rect("Value", rt, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-44, 0), Vector2.zero),
            "-", 16, Ink, TextAnchor.MiddleRight, FontStyle.Bold);
        var bar = rt.gameObject.AddComponent<StatBar>();
        Wire(bar, "label", label);
        Wire(bar, "fill", fill);
        Wire(bar, "value", value);
        return bar;
    }

    static RectTransform InfoRow(Transform parent, float x, float y, float h, string name) =>
        Rect(name, parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(x, -y - h), new Vector2(-24, -y));

    // ------------------------------------------------------------------ helpers

    static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        return rt;
    }

    static Image Img(RectTransform rt, Color c, Sprite sprite = null)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = c;
        if (sprite != null) { img.sprite = sprite; img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple; }
        return img;
    }

    static Text Txt(RectTransform rt, string s, int size, Color c, TextAnchor anchor, FontStyle style = FontStyle.Normal)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = UIFont;
        t.text = s;
        t.fontSize = size;
        t.color = c;
        t.alignment = anchor;
        t.fontStyle = style;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    static void Wire(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogError($"[SceneBuilder] field {field} not found on {target}"); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WireArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Color Hex(string h) => ColorUtility.TryParseHtmlString("#" + h, out var c) ? c : Color.magenta;
}
