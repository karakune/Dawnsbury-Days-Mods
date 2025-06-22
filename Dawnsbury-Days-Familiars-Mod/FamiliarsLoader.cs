using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Familiars;

public static class FamiliarsLoader
{
	[DawnsburyDaysModMainMethod]
	public static void LoadMod()
	{
		foreach (var feat in FamiliarFeats.CreateFeats())
			ModManager.AddFeat(feat);

		foreach (var feat in FamiliarAbilities.CreateFeats())
			ModManager.AddFeat(feat);
	}
}