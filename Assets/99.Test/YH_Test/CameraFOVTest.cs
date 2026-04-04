using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFOVTest : MonoBehaviour
{
    public CinemachineTargetGroup targetGroup;
    public GameObject blockPrefab;
    public Transform mainBody;
    public Vector3 newPos = new Vector3(0, 0, 0);

    private void Update()
    {
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            GameObject block = AddNewBlock();

            AttachNewPart(block);
        }
    }

    public GameObject AddNewBlock()
    {
        GameObject newBlock = Instantiate(blockPrefab, mainBody);

        newBlock.transform.localPosition = newPos;

        return newBlock;
    }

    public void AttachNewPart(GameObject newPart)
    {
        newPart.transform.SetParent(this.transform);

        targetGroup.AddMember(newPart.transform, 1f, 2f);
    }
}
