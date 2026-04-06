using UnityEngine;

public class OrbItem : ObjectPoolBase
{
    private BaseResource innerItem;
    private bool isFixedPos = false;
    public void SetInnerItem(ePoolType type)
    {
        var item = ObjectPoolManager.Instance.OnSpawnResources<BaseResource>();

        if(item == null)
        {
            Debug.LogError("축소화 오브젝트 없음");
            return;
        }

        innerItem = item;
    }

    public override void OnRelease()
    {
        base.OnRelease();
        ObjectPoolManager.Instance.OnRelease(innerItem.key, this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Crafting_Table"))
        {


        }

        if (collision.gameObject.CompareTag("Ship?") && isFixedPos)
        {
            //배에 고정시키기
            isFixedPos = true;
        }
    }
}
