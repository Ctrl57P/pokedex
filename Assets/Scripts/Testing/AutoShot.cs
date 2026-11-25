using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Testing helper, only active when the player is started with "-autoshot <folder>".
// Takes screenshots of: first load, a search, and a selected Pokémon with the shiny view.
public class AutoShot : MonoBehaviour
{
    string folder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-autoshot")
            {
                var go = new GameObject("AutoShot");
                go.AddComponent<AutoShot>().folder = args[i + 1];
            }
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(6f);
        ScreenCapture.CaptureScreenshot($"{folder}/1-list.png");
        yield return new WaitForSeconds(1f);

        var search = FindFirstObjectByType<InputField>();
        search.text = "char";
        yield return new WaitForSeconds(1f);
        ScreenCapture.CaptureScreenshot($"{folder}/2-search.png");
        yield return new WaitForSeconds(1f);

        search.text = "";
        foreach (var row in FindObjectsByType<PokemonRow>())
            if (row.Id == 94) row.GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSeconds(4f);
        ScreenCapture.CaptureScreenshot($"{folder}/3-gengar.png");
        yield return new WaitForSeconds(1f);

        var screen = GameObject.Find("Screen").GetComponent<Button>();
        screen.onClick.Invoke();
        screen.onClick.Invoke();
        yield return new WaitForSeconds(3f);
        ScreenCapture.CaptureScreenshot($"{folder}/4-shiny.png");
        yield return new WaitForSeconds(1f);
        Application.Quit();
    }
}
