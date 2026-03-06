using Dawnsbury.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Weapons;

public class WeaponsLoader
{
	[DawnsburyDaysModMainMethod]
	public static void LoadMod()
	{
		ModManager.RegisterNewItemIntoTheShop("Khakkhara", name => 
			new Item(name, IllustrationName.Quarterstaff, "Khakkhara", 0, 2, Trait.Club, 
				Trait.Uncommon, Trait.Monk, Trait.Shove, Trait.TwoHand1d10, Trait.VersatileP, Trait.Martial, Trait.Mod)
				.WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
		);
	}
}