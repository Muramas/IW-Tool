using System;
using System.Collections;
using System.Collections.Generic;
using CardGame;
using Newtonsoft.Json;
using UnityEngine;

public class JsonLoader : MonoBehaviour
{
	public static JsonLoader instance;

	private int step;

	private List<Action> actions;

	private string path = "JsonFiles/";

	public float progress
	{
		get
		{
			if (actions == null)
			{
				return 0f;
			}
			return (float)step / (float)actions.Count;
		}
	}

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

	public void LoadAll()
	{
		Init();
		StartCoroutine(loadAll());
	}

	public void LoadAllCritical()
	{
		Init();
		for (int i = 0; i < actions.Count; i++)
		{
			actions[i]();
		}
	}

	private void Init()
	{
		actions = new List<Action>
		{
			delegate
			{
				GlobalData.Upgrades = Load<UpgradeFormat>("Upgrades");
			},
			delegate
			{
				GlobalData.Achievements = Load<AchievementFormat>("Achievements");
			},
			delegate
			{
				GlobalData.Triumphs = Load<TriumphFormat>("Triumphs");
			},
			delegate
			{
				GlobalData.Buildings = Load<BuildingFormat>("Buildings");
			},
			delegate
			{
				GlobalData.Spells = Load<SpellFormat>("Spells");
			},
			delegate
			{
				GlobalData.Portraits = Load<PortraitFormat>("Portraits");
			},
			delegate
			{
				GlobalData.PetSkins = Load<PortraitFormat>("PetSkins");
			},
			delegate
			{
				GlobalData.Backgroungs = Load<BackgroundManager.BackgroundFormat>("Backgrounds");
			},
			delegate
			{
				GlobalData.Orbs = Load<OrbManager.OrbFormat>("Orbs");
			},
			delegate
			{
				GlobalData.Cursors = Load<CustomCursorManager.CursorFormat>("Cursors");
			},
			delegate
			{
				GlobalData.MystParagons = Load<ParagonObjectiveData>("MystParagons");
			},
			delegate
			{
				GlobalData.PermanentParagons = Load<ObjectiveData>("PermanentParagons");
			},
			delegate
			{
				GlobalData.TrialMilestones = Load<ObjectiveData>("TrialMilestones");
			},
			delegate
			{
				GlobalData.SpellMastery = Load<ObjectiveData>("SpellMastery");
			},
			delegate
			{
				GlobalData.Intelligence = Load<AttributeFormat>("Intelligence");
			},
			delegate
			{
				GlobalData.Insight = Load<AttributeFormat>("Insight");
			},
			delegate
			{
				GlobalData.Spellcraft = Load<AttributeFormat>("Spellcraft");
			},
			delegate
			{
				GlobalData.Wisdom = Load<AttributeFormat>("Wisdom");
			},
			delegate
			{
				GlobalData.Dominance = Load<AttributeFormat>("Dominance");
			},
			delegate
			{
				GlobalData.Patience = Load<AttributeFormat>("Patience");
			},
			delegate
			{
				GlobalData.Mastery = Load<AttributeFormat>("Mastery");
			},
			delegate
			{
				GlobalData.Empathy = Load<AttributeFormat>("Empathy");
			},
			delegate
			{
				GlobalData.SetBonuses = Load<ItemsCreator.SetDetails>("SetBonuses");
			},
			delegate
			{
				GlobalData.Items = Load<ItemsCreator.ItemDetail>("Items");
			},
			delegate
			{
				GlobalData.EnchantmentBonuses = Load<ItemsCreator.Enchantment>("Enchantment");
			},
			delegate
			{
				GlobalData.Weapons = Load<WeaponManager.WeaponFormat>("Weapons");
			},
			delegate
			{
				GlobalData.Mounts = Load<MountManager.MountFormat>("Mounts");
			},
			delegate
			{
				GlobalData.Phylactery = Load<PhylacteryManager.PhylacteryFormat>("Phylactery");
			},
			delegate
			{
				GlobalData.Mythics = Load<MythicData>("Mythics");
			},
			delegate
			{
				GlobalData.Affixes = Load<AffixData>("Affixes");
			},
			delegate
			{
				GlobalData.Tempering = Load<TemperingData>("Tempering");
			},
			delegate
			{
				GlobalData.Skills = Load<SkillData>("Skills");
			},
			delegate
			{
				GlobalData.RealmUpgrades = Load<RealmManager.UpdateFormat>("RealmUpgrades");
			},
			delegate
			{
				GlobalData.Planes = Load<PlaneFormat>("Planes");
			},
			delegate
			{
				GlobalData.Paramnesics = Load<RealmcraftManager.ParamnesicsFormat>("Paramnesics");
			},
			delegate
			{
				GlobalData.BuildingGilding = Load<BuildingSpecializationFormat>("BuildingGilding");
			},
			delegate
			{
				GlobalData.PantheonGilding = Load<PantheonSpecializationFormat>("PantheonGilding");
			},
			delegate
			{
				GlobalData.Familiars = Load<FamiliarFormat>("Familiars");
			}
		};
	}

	private List<T> Load<T>(string file)
	{
		TextAsset textAsset = Resources.Load<TextAsset>(path + file);
		List<T> result = JsonConvert.DeserializeObject<List<T>>(textAsset.text);
		Resources.UnloadAsset(textAsset);
		return result;
	}

	private IEnumerator loadAll()
	{
		for (step = 0; step < actions.Count; step++)
		{
			yield return null;
			actions[step]();
		}
	}
}
