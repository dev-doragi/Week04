using UnityEngine;
using UnityEngine.InputSystem;

public class MinimapController : MonoBehaviour
{
    public Camera minimapCamera;

    private int currentState = 0;

    private Rect smallRect = new Rect(0.0f, 0.35f, 0.3f, 0.3f);
    private Rect largeRect = new Rect(0.1f, 0.1f, 0.8f, 0.8f);

    void Start()
    {
        ApplyState();
    }

    void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            currentState = (currentState + 1) % 3;
            ApplyState();
        }
    }
    void ApplyState()
    {
        switch (currentState)
        {
            case 0: 
                minimapCamera.enabled = false; 
                Debug.Log("미니맵 꺼짐");
                break;

            case 1: 
                minimapCamera.enabled = true;
                minimapCamera.rect = smallRect;
                Debug.Log("미니맵 작게");
                break;

            case 2:
                minimapCamera.enabled = true;
                minimapCamera.rect = largeRect;
                Debug.Log("미니맵 크게");
                break;
        }
    }
}
