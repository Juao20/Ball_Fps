using UnityEngine;
using Mirror;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private Behaviour[] componentsToDisable;
    [SerializeField] private Camera sceneCamera;

    [SerializeField] private GameObject gun;
    [SerializeField] private GameObject Eyes;
    private void Start()
    {
        if (!isLocalPlayer)
        {
            foreach (Behaviour component in componentsToDisable)
            {
                if (component != null)
                    component.enabled = false;
            }
            SetLayerRecursively(gun, 0);
            SetLayerRecursively(Eyes, 0);
        }
        else
        {
            SetLayerRecursively(gun, 6);
            SetLayerRecursively(Eyes,7);
        }
    }

    public override void OnStartLocalPlayer()
    {
        if (sceneCamera == null)
        {
            sceneCamera = FindSceneCamera();
        }

        if (sceneCamera != null)
        {
            sceneCamera.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PlayerSetup: no scene camera found. Assign the scene camera in the inspector or make sure one is tagged MainCamera.", this);
        }
    }

    private void OnDisable()
    {
        if (sceneCamera != null)
        {
            sceneCamera.gameObject.SetActive(true);
        }
    }

    private Camera FindSceneCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            return mainCamera;

        foreach (Camera camera in Camera.allCameras)
        {
            if (camera != null && camera.enabled)
            {
                return camera;
            }
        }

        return null;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null)
            return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
