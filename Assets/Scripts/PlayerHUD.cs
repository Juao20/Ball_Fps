using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private static PlayerHUD instance;

    private Text healthText;
    private Text respawnText;
    private Text matchEndText;
    private Image damageOverlay;
    private PlayerStats bound;

    private int lastHealth = -1;
    private float damageFlashTimeLeft = 0f;
    private const float damageFlashDuration = 0.4f;
    private const float damageFlashMaxAlpha = 0.45f;

    private Image hitMarkerLine1;
    private Image hitMarkerLine2;
    private float hitMarkerTimeLeft = 0f;
    private const float hitMarkerDuration = 0.25f;
    private static readonly Color hitMarkerColor = Color.white;
    private static readonly Color hitMarkerKillColor = new Color(1f, 0.2f, 0.15f);

    public static void Bind(PlayerStats localStats)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("PlayerHUD");
            instance = go.AddComponent<PlayerHUD>();
            instance.BuildUI();
        }
        instance.Attach(localStats);
    }

    public static void ShowRespawnMessage(bool show, float seconds)
    {
        if (instance == null || instance.respawnText == null) return;

        instance.respawnText.gameObject.SetActive(show);
        if (show)
            instance.respawnText.text = seconds > 0 ? $"VOUS ÊTES MORT\nRespawn dans {seconds:0}s" : "VOUS ÊTES MORT";
    }

    public static void ShowMatchEnd(string winnerText)
    {
        if (instance == null || instance.matchEndText == null) return;

        instance.matchEndText.text = winnerText;
        instance.matchEndText.gameObject.SetActive(true);
    }

    public static void ShowHitMarker(bool isKill = false)
    {
        if (instance == null) return;
        instance.DisplayHitMarker(isKill);
    }

    private void Attach(PlayerStats stats)
    {
        if (bound != null)
            bound.OnHealthUpdated -= UpdateHealth;

        bound = stats;
        bound.OnHealthUpdated += UpdateHealth;
        lastHealth = stats.CurrentHealth; // avoid a false flash on (re)bind
        UpdateHealth(stats.CurrentHealth, stats.MaxHealth);
        ShowRespawnMessage(false, 0);
    }

    private void Update()
    {
        if (damageOverlay != null && damageFlashTimeLeft > 0f)
        {
            damageFlashTimeLeft -= Time.deltaTime;
            float alpha = Mathf.Clamp01(damageFlashTimeLeft / damageFlashDuration) * damageFlashMaxAlpha;
            Color c = damageOverlay.color;
            c.a = alpha;
            damageOverlay.color = c;
        }

        if (hitMarkerTimeLeft > 0f)
        {
            hitMarkerTimeLeft -= Time.deltaTime;
            float alpha = Mathf.Clamp01(hitMarkerTimeLeft / hitMarkerDuration);
            SetHitMarkerAlpha(alpha);
        }
    }

    private void DisplayHitMarker(bool isKill)
    {
        if (hitMarkerLine1 == null || hitMarkerLine2 == null) return;

        Color c = isKill ? hitMarkerKillColor : hitMarkerColor;
        hitMarkerLine1.color = c;
        hitMarkerLine2.color = c;
        hitMarkerTimeLeft = hitMarkerDuration;
        SetHitMarkerAlpha(1f);
    }

    private void SetHitMarkerAlpha(float alpha)
    {
        Color c1 = hitMarkerLine1.color;
        c1.a = alpha;
        hitMarkerLine1.color = c1;

        Color c2 = hitMarkerLine2.color;
        c2.a = alpha;
        hitMarkerLine2.color = c2;
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("PlayerHUDCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject overlayGO = new GameObject("DamageOverlay", typeof(RectTransform));
        overlayGO.transform.SetParent(canvasGO.transform, false);
        RectTransform ort = overlayGO.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;
        damageOverlay = overlayGO.AddComponent<Image>();
        damageOverlay.color = new Color(1f, 0f, 0f, 0f);
        damageOverlay.raycastTarget = false;

        hitMarkerLine1 = CreateHitMarkerLine(canvasGO.transform, 45f);
        hitMarkerLine2 = CreateHitMarkerLine(canvasGO.transform, -45f);

        healthText = CreateText(canvasGO.transform);
        RectTransform hrt = healthText.rectTransform;
        hrt.anchorMin = new Vector2(0, 0);
        hrt.anchorMax = new Vector2(0, 0);
        hrt.pivot = new Vector2(0, 0);
        hrt.anchoredPosition = new Vector2(20, 20);
        hrt.sizeDelta = new Vector2(300, 40);
        healthText.fontSize = 24;
        healthText.alignment = TextAnchor.LowerLeft;

        respawnText = CreateText(canvasGO.transform);
        RectTransform rrt = respawnText.rectTransform;
        rrt.anchorMin = new Vector2(0.5f, 0.5f);
        rrt.anchorMax = new Vector2(0.5f, 0.5f);
        rrt.pivot = new Vector2(0.5f, 0.5f);
        rrt.anchoredPosition = Vector2.zero;
        rrt.sizeDelta = new Vector2(500, 100);
        respawnText.fontSize = 32;
        respawnText.alignment = TextAnchor.MiddleCenter;
        respawnText.color = Color.red;
        respawnText.gameObject.SetActive(false);

        matchEndText = CreateText(canvasGO.transform);
        RectTransform mrt = matchEndText.rectTransform;
        mrt.anchorMin = new Vector2(0.5f, 0.5f);
        mrt.anchorMax = new Vector2(0.5f, 0.5f);
        mrt.pivot = new Vector2(0.5f, 0.5f);
        mrt.anchoredPosition = new Vector2(0, 120);
        mrt.sizeDelta = new Vector2(700, 100);
        matchEndText.fontSize = 40;
        matchEndText.alignment = TextAnchor.MiddleCenter;
        matchEndText.color = Color.yellow;
        matchEndText.gameObject.SetActive(false);
    }

    private Image CreateHitMarkerLine(Transform parent, float angle)
    {
        GameObject go = new GameObject("HitMarkerLine", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(4f, 22f);
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;

        return img;
    }

    private Text CreateText(Transform parent)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private void UpdateHealth(int current, int max)
    {
        if (healthText != null)
            healthText.text = $"PV: {current} / {max}";

        if (lastHealth >= 0 && current < lastHealth)
            damageFlashTimeLeft = damageFlashDuration;

        lastHealth = current;
    }
}