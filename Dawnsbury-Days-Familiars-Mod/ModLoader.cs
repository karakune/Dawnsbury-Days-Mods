using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Controls.Statblocks;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.DeployableFamiliars;

public static class ModLoader
{
	[DawnsburyDaysModMainMethod]
	public static void LoadMod()
	{
		ModData.LoadData();
		FamiliarFeats.Load();
		FamiliarAbilities.Load();
		
		foreach (var feat in FamiliarAbilities.CreateFeats())
			ModManager.AddFeat(feat, ModData.Traits.ModName);
		
		foreach (var feat in FamiliarMasterDedication.CreateFeats())
			ModManager.AddFeat(feat, ModData.Traits.ModName);
		
		// TODO: Complete section generator
		// Add tags to separate abilities, master abilities, and familiar actions.
		CreatureStatblock.CreatureStatblockSectionGenerators.Add(new CreatureStatblockSectionGenerator(
			"Deployable familiar",
			self =>
			{
				string displayedName;
				string displayedIcon;
				if (DeployableFamiliarTag.FindTag(self) is { } fTag)
				{
					displayedName = fTag.FamiliarName ?? "Familiar";
					displayedIcon = fTag.IllustrationOrDefault.IllustrationAsIconString;
				}
				// Decided to not generate on the familiar itself due to excessive formatting.
				// Also because it makes sense to display these feats on the familiar in its abilities section,
				// instead of as summaries like it is on the master.
				/*else if (DeployableFamiliarTag.FindMaster(self) is { } master)
				{
					displayedName = master.Name;
					displayedIcon = master.Illustration.IllustrationAsIconString;
				}*/
				else
					return null;
				List<Feat> familiarFeats = self.PersistentCharacterSheet?.Calculated.AllFeats
					.Where(ft => ft.HasTrait(Trait.CombatFamiliarAbility))
					.ToList() ?? [];
				List<string> abilities = familiarFeats.Select(ft => ft.Name).ToList();
				abilities.Sort();
				string deployment = self.HasFeat(ModData.FeatNames.AutoDeployNo)
					? "Hidden"
					: self.HasFeat(ModData.FeatNames.AutoDeployYes)
						? "Deployed"
						: "(UNSELECTED)";
				return $$"""
				         {b}Name{/b} {{displayedIcon}} {{displayedName}}
				         {{(abilities.Count > 0 ? "{b}Abilities{/b} " + string.Join(", ", abilities) : null)}}
				         {i}{{deployment}} at the start of combat.{/i}
				         """;
			}));
	}

	extension(ModManager)
	{
		/// <summary>
		/// Creates a custom "Mod" trait which indicates which mod the traited content comes from. This trait is visible with a basic description that uses your humanized mod name.
		/// </summary>
		/// <param name="modTechnicalName">The technicalName of the mod such as "MoreDedications". The final technical name of this trait will be "Mod:MoreDedications".</param>
		/// <param name="modName">The humanized name of the mod such as "More Dedications".</param>
		public static Trait RegisterModNameTrait(string modTechnicalName, string modName)
		{
			return ModManager.RegisterTrait(
				"Mod:" + modTechnicalName,
				new TraitProperties(
					"Mod",
					true,
					"This content comes from or is modified by {b}" + modName + "{/b}.",
					false,
					Color.LightSteelBlue,
					false,
					false));
		}

		/// <summary>
		/// As <see cref="ModManager.AddFeat"/>, but it removes the Mod trait and adds your mod's specific trait.
		/// </summary>
		/// <param name="newFeat">The feat to register.</param>
		/// <param name="modName">The mod-source trait to replace the "Mod" trait with.</param>
		public static void AddFeat(Feat newFeat, Trait modName)
		{
			ModManager.AddFeat(newFeat);
			newFeat.Traits.Remove(Trait.Mod);
			newFeat.Traits.Insert(0, modName);
		}
	}
}