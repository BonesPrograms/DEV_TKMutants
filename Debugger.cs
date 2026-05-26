
using HarmonyLib;
using XRL.World;
using XRL.Wish;
using XRL;

//This is the debugger for checking that the Glimmer of Truth achievement is disabled.
//How to use:
//0. first you must have at least 20 base glimmer. stat:Ego:100, a few mutations, and some levels will help
//1. wish "tkrst"
//2. wish "xp:99999" (your glimmer status updates on levelup)
//3. check if you are calling unlock

[HarmonyPatch(typeof(GameObject), nameof(GameObject.IsMutant))]

[HasWishCommand]

class IsMutantDebugger
{
    static bool DebugTK = false; //if this is set to true, then you will always return false on IsMutant checks

    [WishCommand("tkrst")]
    static void TKReset()
    {
        GameObject obj = The.Player;
        obj.SetIntProperty("LastGlimmer", 1);
        IComponent<GameObject>.AddPlayerMessage($"LastGlimmer set to {obj.GetIntProperty("LastGlimmer")}");
    }

    [WishCommand("tkdbg")]
    static void SetDebugTK()
    {
        DebugTK = !DebugTK;
        IComponent<GameObject>.AddPlayerMessage($"DebugTK is {DebugTK}");
    }

    [HarmonyPostfix]
    public static void Postfix(ref bool __result)
    {
        if (DebugTK)
            __result = false;
    }
}

[HarmonyPatch(typeof(AchievementInfo), nameof(AchievementInfo.Unlock))]
class UnlockDebugger
{
    [HarmonyPrefix]
    public static void Prefix(AchievementInfo __instance)
    {
        if (__instance.Name.Contains("Glimmer"))
            XRL.UI.Popup.Show($"Called Unlock on {__instance.Name}!");
    }
}
