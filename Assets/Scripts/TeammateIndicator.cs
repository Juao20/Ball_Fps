using UnityEngine;
using UnityEngine.UI;

// Shows a small blue marker above teammates' heads (Team Deathmatch only) so you can tell
// them apart from enemies at a glance. Hidden for yourself, for enemies, and outside TDM.
public class TeammateIndicator : MonoBehaviour
{
    [SerializeField] private Transform headAnchor;

    private PlayerStats stats;
    private Canvas canvas;
    private RectTransform dotRect;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("TeammateIndicatorCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject dotGO = new GameObject("Dot", typeof(RectTransform));
        dotGO.transform.SetParent(canvasGO.transform, false);
        dotRect = dotGO.GetComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(14f, 14f);

        Image dotImage = dotGO.AddComponent<Image>();
        dotImage.color = new Color(0.15f, 0.5f, 1f, 1f); // blue

        canvasGO.SetActive(false);
    }

    private void LateUpdate()
    {
        if (stats == null || headAnchor == null || canvas == null) return;

        PlayerStats localStats = FindLocalPlayerStats();
        Camera cam = PlayerAim.LocalCamera;

        bool show = localStats != null && cam != null && localStats != stats
            && stats.team != 0 && stats.team == localStats.team;

        if (!show)
        {
            canvas.gameObject.SetActive(false);
            return;
        }

        Vector3 viewportPoint = cam.WorldToViewportPoint(headAnchor.position);
        bool inFront = viewportPoint.z > 0f
            && viewportPoint.x > 0f && viewportPoint.x < 1f
            && viewportPoint.y > 0f && viewportPoint.y < 1f;

        canvas.gameObject.SetActive(inFront);
        if (!inFront) return;

        dotRect.position = cam.WorldToScreenPoint(headAnchor.position);
    }

    private static PlayerStats FindLocalPlayerStats()
    {
        foreach (PlayerStats p in PlayerStats.Players)
        {
            if (p != null && p.isLocalPlayer) return p;
        }
        return null;
    }
}
