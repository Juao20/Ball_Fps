using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardUI : MonoBehaviour
{
    private static ScoreboardUI instance;
    private Text scoreText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("ScoreboardUI");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ScoreboardUI>();
    }

    private void Awake()
    {
        BuildUI();
        PlayerStats.ScoreboardChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        PlayerStats.ScoreboardChanged -= Refresh;
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("ScoreboardCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject textGO = new GameObject("ScoreText", typeof(RectTransform));
        textGO.transform.SetParent(canvasGO.transform, false);
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta = new Vector2(220, 200);

        scoreText = textGO.AddComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 18;
        scoreText.alignment = TextAnchor.UpperRight;
        scoreText.color = Color.white;
        scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
        scoreText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private void Refresh()
    {
        if (scoreText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("SCORES");
        foreach (PlayerStats p in PlayerStats.Players.OrderByDescending(p => p.kills))
        {
            sb.AppendLine($"{p.playerName}   {p.kills}/{p.deaths}");
        }
        scoreText.text = sb.ToString();
    }
}
