using Unity.IntegerTime;
using UnityEngine;

public class Furnace : MonoBehaviour
{
    [SerializeField] private float maxFuelGauge = 100;
    [SerializeField] private float fuelGauge = 100;
    public float FuelGauge => fuelGauge;
    [SerializeField] private float fuelEfficiency = 1;
    [SerializeField] FireIntensityController controller;
    private void FixedUpdate()
    {
        DecreaseFuel();
    }

    public void OnInteractItem(Wood wood)
    {
        var addPoint = wood.GetRefuelAmount();
        fuelGauge = Mathf.Max(fuelGauge, fuelGauge + addPoint);
        SetFuranceFire(fuelGauge);
        SetFuranceUI(fuelGauge);
    }

    public void SetFuranceFire(float amount)
    {
        var ratio = amount / maxFuelGauge;
        controller.SetIntensity(ratio + 0.5f);
    }

    public void SetFuranceUI(float amount)
    {
        var ui = UIManager.Instance.GetUI<GaugeController>();
        ui.Apply(amount);
    }

    public void DecreaseFuel()
    {
        fuelGauge -= Time.fixedDeltaTime * fuelEfficiency;
        fuelGauge = Mathf.Max(0, fuelGauge);
        SetFuranceFire(fuelGauge);
        SetFuranceUI(fuelGauge);

        if (fuelGauge <= 0)
        {
            //TODO 실패엔딩
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerInteraction player))
            {
                player.OnChangedInteractionState(ePlayerState.Fueling);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerInteraction player))
            {
                player.OnChangedInteractionState(ePlayerState.None);
            }
        }
    }
}
