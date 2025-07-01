using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Witch;

public static class ClassFeats
{
	public static IEnumerable<Feat> CreateFeats()
	{
		yield return new TrueFeat(ModManager.RegisterFeatName("Cackle"), 1,
			"Your patron’s power fills you with confidence, letting you sustain a magical working even as a quick burst of laughter leaves your lips.",
			"You learn the cackle hex.", [WitchLoader.TWitch])
			.WithOnSheet(sheet => sheet.AddFocusSpellAndFocusPoint(WitchSpells.THex, Ability.Intelligence, WitchSpells.Cackle)
		);
	}
}