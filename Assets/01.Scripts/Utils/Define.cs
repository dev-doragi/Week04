using UnityEngine;

public enum eItemType
{
    None = -1,
    Wood,
    WetWood,
    Fabric,
    Block,
    Catcher,
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
    Steering
}