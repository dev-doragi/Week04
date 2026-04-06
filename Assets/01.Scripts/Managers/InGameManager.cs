using UnityEngine;

public class InGameManager : MonoBehaviour
{
    private static InGameManager instance;
    public static InGameManager Instance { get { return instance; } private set { instance = value; } }
    [SerializeField] BoatSteeringController boatController;
    [SerializeField] PlayerEntity player;
    [SerializeField] Furnace furnace;
    [SerializeField] Crafting_Table table;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    public void OnChangedGameMode()
    {
        boatController.ControllSteer = !boatController.ControllSteer;
        player.InputLock = !player.InputLock;
        //TODO 플레이어 움직임 고정 + 카메라 무빙
        //playerInput.SetCursorState(false);
    }

    public void OnRefuel(Wood wood)
    {
        furnace.OnInteractItem(wood);
    }

    public bool TryCrafting(BaseResource item)
    {
        return table.OnPushItem(item);
    }

    public BaseResource PopResource()
    {
        return table.OnCheckedCrafting();
    }
}
