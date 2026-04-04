using Unity.IntegerTime;
using UnityEngine;

public class Furnace : MonoBehaviour
{
    [SerializeField] private float maxFuelGauge = 100;
    [SerializeField] private float fuelGauge = 100;
    public float FuelGauge => fuelGauge;
    [SerializeField] private float fuelEfficiency = 1;
    FireIntensityController controller;
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
        var result = ratio <= 0 ? 0 : ratio + 0.5f;
        controller.Intensity = ratio;
    }

    public void SetFuranceUI(float amount)
    {

    }

    public void DecreaseFuel()
    {
        fuelGauge -= Time.fixedDeltaTime * fuelEfficiency;
        fuelGauge = Mathf.Max(0, fuelGauge);
        SetFuranceFire(fuelGauge);
        SetFuranceUI(fuelGauge);

        if (fuelGauge <= 0)
        {

        }
    }

}
