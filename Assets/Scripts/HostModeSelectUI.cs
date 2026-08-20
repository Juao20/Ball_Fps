using Mirror;
using UnityEngine;
using UnityEngine.UI;

// Lets the host pick Deathmatch or Team Deathmatch before starting the server.
// Sits next to the stock NetworkManagerHUD (used for Client/Stop). Hides itself
// once a host/server/client session is active.
public class HostModeSelectUI : MonoBehaviour
{
    private static HostModeSelectUI instance;
    private GameObject panel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("HostModeSelectUI");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<HostModeSelectUI>();
    }

    private void Awake()
    {
        BuildUI();
    }

    private void Update()
    {
        if (panel == null) return;
        bool sessionActive = NetworkServer.active || NetworkClient.active;
        if (panel.activeSelf == sessionActive)
            panel.SetActive(!sessionActive);
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("HostModeSelectCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0.5f);
        panelRt.anchorMax = new Vector2(0f, 0.5f);
        panelRt.pivot = new Vector2(0f, 0.5f);
        panelRt.anchoredPosition = new Vector2(20, 140);
        panelRt.sizeDelta = new Vector2(220, 170);

        CreateNicknameField(panel.transform, new Vector2(0, 130));
        CreateButton(panel.transform, "Deathmatch", new Vector2(0, 45), GameMode.Deathmatch);
        CreateButton(panel.transform, "Team Deathmatch", new Vector2(0, 0), GameMode.TeamDeathmatch);
    }

    private void CreateNicknameField(Transform parent, Vector2 anchoredPos)
    {
        GameObject labelGO = new GameObject("NicknameLabel", typeof(RectTransform));
        labelGO.transform.SetParent(parent, false);
        RectTransform labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 1);
        labelRt.anchorMax = new Vector2(0, 1);
        labelRt.pivot = new Vector2(0, 1);
        labelRt.anchoredPosition = anchoredPos;
        labelRt.sizeDelta = new Vector2(220, 20);
        Text label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 14;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.text = "Pseudo :";

        GameObject fieldGO = new GameObject("NicknameField", typeof(RectTransform));
        fieldGO.transform.SetParent(parent, false);
        RectTransform fieldRt = fieldGO.GetComponent<RectTransform>();
        fieldRt.anchorMin = new Vector2(0, 1);
        fieldRt.anchorMax = new Vector2(0, 1);
        fieldRt.pivot = new Vector2(0, 1);
        fieldRt.anchoredPosition = anchoredPos - new Vector2(0, 20);
        fieldRt.sizeDelta = new Vector2(220, 32);

        Image fieldBg = fieldGO.AddComponent<Image>();
        fieldBg.color = new Color(1f, 1f, 1f, 0.9f);

        InputField input = fieldGO.AddComponent<InputField>();
        input.characterLimit = 20;

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(fieldGO.transform, false);
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8, 4);
        textRt.offsetMax = new Vector2(-8, -4);
        Text text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.color = Color.black;
        text.supportRichText = false;

        GameObject placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
        placeholderGO.transform.SetParent(fieldGO.transform, false);
        RectTransform placeholderRt = placeholderGO.GetComponent<RectTransform>();
        placeholderRt.anchorMin = Vector2.zero;
        placeholderRt.anchorMax = Vector2.one;
        placeholderRt.offsetMin = new Vector2(8, 4);
        placeholderRt.offsetMax = new Vector2(-8, -4);
        Text placeholder = placeholderGO.AddComponent<Text>();
        placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholder.fontSize = 16;
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.color = new Color(0f, 0f, 0f, 0.5f);
        placeholder.text = "Entrez votre pseudo";

        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = PlayerPrefs.GetString("PlayerName", "");
        input.onValueChanged.AddListener(SaveNickname);
    }

    private void SaveNickname(string value)
    {
        PlayerPrefs.SetString("PlayerName", value);
    }

    private void CreateButton(Transform parent, string label, Vector2 anchoredPos, GameMode mode)
    {
        GameObject buttonGO = new GameObject(label + "Button", typeof(RectTransform));
        buttonGO.transform.SetParent(parent, false);
        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(220, 40);

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        Button button = buttonGO.AddComponent<Button>();
        button.onClick.AddListener(() => StartHostWithMode(mode));

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
    }

    private void StartHostWithMode(GameMode mode)
    {
        GameModeManager.SelectedMode = mode;
        NetworkManager.singleton.StartHost();
    }
}
