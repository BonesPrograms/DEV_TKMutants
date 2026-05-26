using HarmonyLib;
using XRL.World;
using XRL.World.Parts;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;


namespace TKMutantsHarmony
{
    [HarmonyPatch(typeof(Nectar_Tonic_Applicator), nameof(Nectar_Tonic_Applicator.FireEvent))]

    //The purpuse of this patch is to modify the code from line 59-60 to give +1 MP in addition to +1 AP to true kin.

    //This is what that code looks like in IL:

    // 	IL_0084: ldloc.1
    // IL_0085: ldstr "AP" /* 700393B2 */
    // IL_008a: callvirt instance class XRL.World.Statistic XRL.World.GameObject::GetStat(string) /* 06005CA9 */
    // IL_008f: dup
    // IL_0090: callvirt instance int32 XRL.World.Statistic::get_BaseValue() /* 0600633B */
    // IL_0095: ldloc.0
    // IL_0096: add
    // IL_0097: callvirt instance void XRL.World.Statistic::set_BaseValue(int32) /* 0600633C */
    // IL_009c: ldloc.2
    // IL_009d: ldstr "{{C|You gain " /* 700E2E4B */
    // IL_00a2: ldloc.0
    // IL_00a3: ldstr "attribute point" /* 700E2E67 */
    // IL_00a8: ldnull
    // IL_00a9: call string XRL.Extensions::Things(int32, string, string) /* 060034E7 */
    // IL_00ae: ldstr "!}}" /* 700E2E87 */
    // IL_00b3: call string [mscorlib]System.String::Concat(string, string, string, string) /* 0A0002A1 */
    // IL_00b8: stloc.2
    // IL_00b9: br IL_01a1

    //Our instructions will be getting inserted right after IL_0084. You will see that I pretty much duplicate the IL instruction and the only changes
    //are that I load the different strings onto the stack (because I dont *really* know what Im doing)

    class NectarTranspiler
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> GiveMPOnUse(IEnumerable<CodeInstruction> input)
        {
            List<CodeInstruction> codes = input.ToList();
            int? callIndex = GetInsertIndex(codes);
            if (callIndex is int index)
            {
                GetCalls(out var getStat, out var baseValueGetter, out var baseValueSetter, out var things, out var concat);
                InsertInstructions(codes, index, getStat, baseValueGetter, baseValueSetter, things, concat);
            }
            foreach (var code in codes)
                yield return code;
        }

        static void GetCalls(out MethodInfo getStat, out MethodInfo baseValueGetter, out MethodInfo baseValueSetter, out MethodInfo things, out MethodInfo concat)
        {
            getStat = AccessTools.Method(typeof(GameObject), nameof(GameObject.GetStat));
            baseValueGetter = AccessTools.PropertyGetter(typeof(Statistic), nameof(Statistic.BaseValue));
            baseValueSetter = AccessTools.PropertySetter(typeof(Statistic), nameof(Statistic.BaseValue));
            things = AccessTools.Method(typeof(XRL.Extensions), nameof(XRL.Extensions.Things), new[] { typeof(int), typeof(string), typeof(string) });
            concat = AccessTools.Method(typeof(string), nameof(string.Concat), new[] { typeof(string), typeof(string), typeof(string), typeof(string) });
        }                                                                     

        static void InsertInstructions(List<CodeInstruction> codes, int index, MethodInfo getStat, MethodInfo baseValueGetter, MethodInfo baseValueSetter, MethodInfo things, MethodInfo concat)
        {
            codes.Insert(index, new(OpCodes.Ldloc_1)); //LdLoc_1 is reloaded onto the stack, and the instructions continue from IL_0085 as you see above
            codes.Insert(index, new(OpCodes.Stloc_2));
            codes.Insert(index, new(OpCodes.Call, concat));
            codes.Insert(index, new(OpCodes.Ldstr, "!}}"));
            codes.Insert(index, new(OpCodes.Call, things));
            codes.Insert(index, new(OpCodes.Ldnull));
            codes.Insert(index, new(OpCodes.Ldstr, "mutation point"));
            codes.Insert(index, new(OpCodes.Ldloc_0));
            codes.Insert(index, new(OpCodes.Ldstr, " \n{{G|You gain "));
            codes.Insert(index, new(OpCodes.Ldloc_2));
            codes.Insert(index, new(OpCodes.Callvirt, baseValueSetter));
            codes.Insert(index, new(OpCodes.Add));
            codes.Insert(index, new(OpCodes.Ldloc_0));
            codes.Insert(index, new(OpCodes.Callvirt, baseValueGetter));
            codes.Insert(index, new(OpCodes.Dup));
            codes.Insert(index, new(OpCodes.Callvirt, getStat));
            codes.Insert(index, new(OpCodes.Ldstr, "MP"));
        }

        static int? GetInsertIndex(List<CodeInstruction> codes)
        {
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode == OpCodes.Ldstr && code.operand is string txt && txt == "AP")
                    return i;
            }
            return null;
        }
    }


}