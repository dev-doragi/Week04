using TMPro;
using UnityEngine;

public class UIItemBar : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text woodTxt;
    [SerializeField] private TMP_Text wetWoodTxt;
    [SerializeField] private TMP_Text fabricTxt;
    [SerializeField] private TMP_Text woodBlockTxt;

    private void Start()
    {
        // RepoManager의 이벤트에 UI 갱신 함수 연결
        if (RepoManager.Instance != null)
        {
            RepoManager.Instance.OnResourceChanged += UpdateResourceUI;

            // 초기값 세팅 (게임 시작 시 현재 배 위에 있는 수량 표시)
            RefreshAllUI();
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제 (메모리 누수 및 에러 방지)
        if (RepoManager.Instance != null)
        {
            RepoManager.Instance.OnResourceChanged -= UpdateResourceUI;
        }
    }

    // 자원 수량이 변할 때마다 호출됨
    private void UpdateResourceUI(ePoolType key, int count)
    {
        switch (key)
        {
            case ePoolType.Wood:
                if (woodTxt != null) woodTxt.text = count.ToString();
                break;
            case ePoolType.WetWood:
                if (wetWoodTxt != null) wetWoodTxt.text = count.ToString();
                break;
            case ePoolType.Fabric:
                if (fabricTxt != null) fabricTxt.text = count.ToString();
                break;
            case ePoolType.BuildWoodBlock:
                if (woodBlockTxt != null) woodBlockTxt.text = count.ToString();
                break;
        }
    }

    // 전체 UI 강제 새로고침 (필요 시 호출)
    public void RefreshAllUI()
    {
        UpdateResourceUI(ePoolType.Wood, RepoManager.Instance.GetResourceCount(ePoolType.Wood));
        UpdateResourceUI(ePoolType.WetWood, RepoManager.Instance.GetResourceCount(ePoolType.WetWood));
        UpdateResourceUI(ePoolType.Fabric, RepoManager.Instance.GetResourceCount(ePoolType.Fabric));
        UpdateResourceUI(ePoolType.BuildWoodBlock, RepoManager.Instance.GetResourceCount(ePoolType.BuildWoodBlock));
    }
}
