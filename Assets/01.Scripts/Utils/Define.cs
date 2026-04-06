using UnityEngine;

public enum ePoolType
{
    None = -1,
    Wood,
    WetWood,
    Fabric,
    WoodBlock,
    Catcher,
    PoofRealistic,
    PoofCartoon,
    Hitting,
    Break,
    HitBase,
    BuildWoodBlock,
    Net,
    Rock,
    Refuel,
    Max
}

public enum eWoodState
{
    Dried,
    Drying,
    Wet
}

public enum ePlayerState
{
    None,
    Fueling,
    Crafting,
    Steering,
    Pickup
}