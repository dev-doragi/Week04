using UnityEngine;

public enum ePoolType
{
    None = -1,
    Wood,
    WetWood,
    Fabric,
    Block,
    Catcher,
    PoofRealistic,
    PoofCartoon,
    Hitting,
    Break,
    HitBase,
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