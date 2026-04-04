using UnityEngine;
using DG.Tweening;

public class PlayerInteraction : MonoBehaviour
{
    public float swingAngle = 80f;
    public float swingDuration = 0.2f;
    public float returnDuration = 0.15f;

    private bool _isSwinging = false;

    public void BoatBreaker(Transform axe)
    {
        if (!_isSwinging)
        {
            Swing(axe);
        }
    }

    private void Swing(Transform axe)
    {
        _isSwinging = true;

        Sequence sequence = DOTween.Sequence();

        // 앞으로 휘두르기
        sequence.Append(axe.DOLocalRotate(
            new Vector3(swingAngle, 0f, 0f), swingDuration)
            .SetEase(Ease.OutQuart));

        // 원래 위치로 복귀
        sequence.Append(axe.DOLocalRotate(
            Vector3.zero, returnDuration)
            .SetEase(Ease.InOutQuad));

        sequence.OnComplete(() => _isSwinging = false);
    }
}
