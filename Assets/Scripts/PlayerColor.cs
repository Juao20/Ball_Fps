using Mirror;
using UnityEngine;

// Gives each player a random body color (server-picked, synced to everyone) so players
// can be told apart at a glance.
public class PlayerColor : NetworkBehaviour
{
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Material[] colorMaterials;

    [SyncVar(hook = nameof(OnColorIndexChanged))]
    private int colorIndex;

    public override void OnStartServer()
    {
        if (colorMaterials != null && colorMaterials.Length > 0)
            colorIndex = Random.Range(0, colorMaterials.Length);
    }

    public override void OnStartClient()
    {
        ApplyColor(colorIndex);
    }

    private void OnColorIndexChanged(int oldIndex, int newIndex)
    {
        ApplyColor(newIndex);
    }

    private void ApplyColor(int index)
    {
        if (bodyRenderer == null || colorMaterials == null || colorMaterials.Length == 0) return;
        index = Mathf.Clamp(index, 0, colorMaterials.Length - 1);
        bodyRenderer.material = colorMaterials[index];
    }
}
