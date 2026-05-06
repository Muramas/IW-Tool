using System.Collections.Generic;
using CardGame;

public static class GlobalData
{
	public static List<UpgradeFormat> Upgrades;

	public static List<AchievementFormat> Achievements;

	public static List<TriumphFormat> Triumphs;

	public static List<BuildingFormat> Buildings;

	public static List<SpellFormat> Spells;

	public static List<PortraitFormat> Portraits;

	public static List<PortraitFormat> PetSkins;

	public static List<BackgroundManager.BackgroundFormat> Backgroungs;

	public static List<OrbManager.OrbFormat> Orbs;

	public static List<CustomCursorManager.CursorFormat> Cursors;

	public static List<ParagonObjectiveData> MystParagons;

	public static List<ObjectiveData> PermanentParagons;

	public static List<ObjectiveData> TrialMilestones;

	public static List<ObjectiveData> SpellMastery;

	public static List<AttributeFormat> Intelligence;

	public static List<AttributeFormat> Insight;

	public static List<AttributeFormat> Spellcraft;

	public static List<AttributeFormat> Wisdom;

	public static List<AttributeFormat> Dominance;

	public static List<AttributeFormat> Patience;

	public static List<AttributeFormat> Mastery;

	public static List<AttributeFormat> Empathy;

	public static List<ItemsCreator.SetDetails> SetBonuses;

	public static List<ItemsCreator.ItemDetail> Items;

	public static List<ItemsCreator.Enchantment> EnchantmentBonuses;

	public static List<WeaponManager.WeaponFormat> Weapons;

	public static List<MountManager.MountFormat> Mounts;

	public static List<PhylacteryManager.PhylacteryFormat> Phylactery;

	public static List<MythicData> Mythics;

	public static List<AffixData> Affixes;

	public static List<TemperingData> Tempering;

	public static List<SkillData> Skills;

	public static List<RealmManager.UpdateFormat> RealmUpgrades;

	public static List<PlaneFormat> Planes;

	public static List<RealmcraftManager.ParamnesicsFormat> Paramnesics;

	public static List<BuildingSpecializationFormat> BuildingGilding;

	public static List<PantheonSpecializationFormat> PantheonGilding;

	public static List<FamiliarFormat> Familiars;

	public static void Clear()
	{
		Upgrades = null;
		Achievements = null;
		Triumphs = null;
		Buildings = null;
		Spells = null;
		Portraits = null;
		Intelligence = null;
		Insight = null;
		Spellcraft = null;
		Wisdom = null;
		Dominance = null;
		Patience = null;
		Mastery = null;
		Empathy = null;
		SetBonuses = null;
		Items = null;
		EnchantmentBonuses = null;
		Weapons = null;
		RealmUpgrades = null;
		Planes = null;
		Paramnesics = null;
		BuildingGilding = null;
		PantheonGilding = null;
		Familiars = null;
	}
}
