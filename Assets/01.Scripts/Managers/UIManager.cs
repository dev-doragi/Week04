using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance { get { return instance; } private set { instance = value; } }

    [SerializeField] private List<UIBase> uiList = new();
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    //UI 열 때
    public T ShowUI<T>() where T : UIBase
    {
        var ui = uiList.Find(x => x is T) as T;
        if (ui == null)
        {
            Debug.LogError($"{typeof(T)} UI 없음");
            return null;
        }
        ui.gameObject.SetActive(true);
        ui.Setup();
        return ui as T;
    }

    //UI 닫을 때
    public void HideUI<T>() where T : UIBase
    {
        var ui = uiList.Find(x => x is T);

        if (ui != null && ui.gameObject.activeSelf)
        {
            ui.Hide();
            ui.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"{typeof(T).Name}를 찾을 수 없어 끄지 못했습니다.");
        }
    }

    //정보만 가져올때
    public T GetUI<T>() where T : UIBase
    {
        var ui = uiList.Find(x => x is T);
        if (ui == null)
        {
            Debug.LogError($"{typeof(T)} UI 없음");
            return null;
        }
        return ui as T;
    }
}
