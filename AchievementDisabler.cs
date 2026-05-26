using HarmonyLib;
using XRL.World.Capabilities;
using XRL.World;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace TKMutantsHarmony
{

	//this patches the conditional statement: if (who.IsTrueKin()) which can be seen at line 43 of PsychicGlimmer.Update()
	//the purpose of this patch is to make the code function like this: if (who.IsTrueKin() && !who.IsMutant()) 


	//this here is what the conditional statement looks like in IL

	// IL_0137: ldarg.0
	// IL_0138: callvirt instance bool XRL.World.GameObject::IsTrueKin() /* 06005E60 */
	// IL_013d: brfalse.s IL_0149

	//our code will be getting inserted between IL_0137 and IL_0138, right after the gameobject is loaded onto the stack

	[HarmonyPatch(typeof(PsychicGlimmer), nameof(PsychicGlimmer.Update))]
	class UpdateTranspiler
	{

		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> CheckIfMutant(IEnumerable<CodeInstruction> input)
		{
			List<CodeInstruction> codes = input.ToList();
			Label? jumpLabel = GetJumpLabel(codes, out int? callIndex); //direct casting here was giving a compilation error
			if (jumpLabel is Label validLabel && callIndex is int index)
			{
				MethodBase isMutant = AccessTools.Method(typeof(GameObject), nameof(GameObject.IsMutant));
																//4 and the instruction continues from IL_0138 as you see above												
				codes.Insert(index, new(OpCodes.Ldarg_0)); //3 if value is false, gameobject parameter is loaded back onto the stack
				codes.Insert(index, new(OpCodes.Brtrue_S, validLabel)); //2 if the value is true then it will jump to the jumplabel
				codes.Insert(index, new(OpCodes.Callvirt, isMutant)); //1 IsMutant() bool is called on gameobject
			}

			foreach (var code in codes)
			{
				yield return code;
			}
		}

		static Label? GetJumpLabel(List<CodeInstruction> codes, out int? index) //this method gets the label for the instruction that the IL jumps to if you fail the condition
		{
			MethodBase isTK = AccessTools.Method(typeof(GameObject), nameof(GameObject.IsTrueKin));
			index = null;
			foreach (var code in codes)
			{
				if (code.opcode == OpCodes.Callvirt && code.operand is MethodBase method && method == isTK)
				{
					index = codes.IndexOf(code);
					//method call happens right before the jump instruction
				}
				if (index != null && code.opcode == OpCodes.Brfalse_S) //so its easy to check for and get assuming someone hasnt inserted another Brfalse_S
				{														//between here and the IsTrueKin call
					return (Label)code.operand;
				}
			}
			return null;

		}
	}



}