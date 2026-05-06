using System;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
	public class BuffSave
	{
		public Dictionary<int, double> progression;

		public Dictionary<int, float> timers;
	}

	public BuffProgressive Profit;

	public BuffProgressive Shards;

	public BuffProgressive Crit;

	public BuffProgressive Void;

	public BuffAttProgressive Attributes;

	public Booster Gathering;

	public Booster Crafting;

	public Booster Praying;

	public Booster Mentalizing;

	public Buff Trials;

	public Buff Catas;

	public Buff Bats;

	public Buff AutoExpedition;

	public VariableComplex BoosterPower;

	public VariableFloat SaveExpPart;

	public VariableFloat BuffScale;

	public Action<BigNumber> OnSpendTime;

	private Dictionary<string, Buff> map;

	private Dictionary<int, BuffProgressive> progressives;

	public void Init()
	{
		BoosterPower = new VariableComplex(1.0);
		SaveExpPart = new VariableFloat(1f);
		BuffScale = new VariableFloat(1f);
		GameContext.ContextAddResource("Booster.Power", BoosterPower);
		GameContext.ContextAddResource("Buff.SaveExpPart", SaveExpPart);
		map = new Dictionary<string, Buff>();
		progressives = new Dictionary<int, BuffProgressive>();
		Profit.effect = new List<SimpleEffect>
		{
			new SimpleEffect(GameManager.Instance.Profit, 0.0, 1.75, EffectNames.Linear, 1.06f)
		};
		Profit.Init(1, "Buff production", "[Profits] +#");
		Profit.SetGrowth(BuffScale);
		map.Add(ResourcesGame.BuffProfit.ToString(), Profit);
		progressives.Add(Profit.ID, Profit);
		Shards.effect = new List<SimpleEffect>
		{
			new SimpleEffect(GameManager.Instance.Scrolls.ShardsPassive, 0.0, 2.0, EffectNames.Linear, 1.06f),
			new SimpleEffect(GameManager.Instance.Scrolls.MaxCharge, 1.0, 1.0, EffectNames.Linear, 0f)
		};
		Shards.Init(2, "Buff passive shards", "[PassiveShardGain] +#\n[AddCharges] +1");
		Shards.SetGrowth(BuffScale);
		map.Add(ResourcesGame.BuffShard.ToString(), Shards);
		progressives.Add(Shards.ID, Shards);
		Crit.effect = new List<SimpleEffect>
		{
			new SimpleEffect(GameManager.Instance.Orb.autoclick_profit, 0.0, 2.0, EffectNames.Linear, 1.06f),
			new SimpleEffect(GameManager.Instance.Orb.crit_chance, 10.0, 1.0, EffectNames.Linear, 0f)
		};
		Crit.Init(3, "Buff crit chance and crit profit", "[AutoclickProfit] +# \n[CritChance] +10%");
		Crit.SetGrowth(BuffScale);
		map.Add(ResourcesGame.BuffClick.ToString(), Crit);
		progressives.Add(Crit.ID, Crit);
		Void.effect = new List<SimpleEffect>
		{
			new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 0.0, 1.5, EffectNames.Linear, 1.06f),
			new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed, 0.0, 1.5, EffectNames.Linear, 0f)
		};
		Void.Init(4, "Buff spawn rate and bonus voidmana from Void Entities", "[VPE] +# \n[EntitySpawnrate] +50%");
		Void.SetGrowth(BuffScale);
		map.Add(ResourcesGame.BuffVoid.ToString(), Void);
		progressives.Add(Void.ID, Void);
		VariableBignumber param = new VariableBignumber(1.0);
		Attributes.effect = new List<SimpleEffect>
		{
			new SimpleEffect(GameManager.Instance.AttributeManager.AttributePower, 0.0, 0.023240000009536743, param, EffectNames.Linear, 0.5f),
			new SimpleEffect(GameManager.Instance.AttributeManager.BonusToAllMem, 5.0, 1.0, EffectNames.Linear, 0f)
		};
		Attributes.Init(5, "Buff attributes", "[AttributePerPoint] +# \n[AllAttributesBonus] +5");
		Attributes.SetGrowth(BuffScale);
		Attributes.Weight = 0.1f;
		map.Add(ResourcesGame.BuffAttribute.ToString(), Attributes);
		progressives.Add(Attributes.ID, Attributes);
		Gathering.effect = new List<SimpleEffect>();
		Gathering.effect.Add(new SimpleEffect(GameManager.Instance.SkillManager.Get(SkillManager.SkillNames.Gathering).boost, 50.0, 1.0));
		Gathering.Init(10, "Boost Gathering Skill", "GatheringBoostTooltip", BoosterPower);
		map.Add(ResourcesGame.BoostGathering.ToString(), Gathering);
		Crafting.effect = new List<SimpleEffect>();
		Crafting.effect.Add(new SimpleEffect(GameManager.Instance.SkillManager.Get(SkillManager.SkillNames.Crafting).boost, 25.0, 1.0));
		Crafting.Init(11, "Boost Crafting Skill", "CraftingBoostTooltip", BoosterPower);
		map.Add(ResourcesGame.BoostCrafting.ToString(), Crafting);
		Praying.effect = new List<SimpleEffect>();
		Praying.effect.Add(new SimpleEffect(GameManager.Instance.SkillManager.Get(SkillManager.SkillNames.Praying).boost, 30.0, 1.0));
		Praying.Init(12, "Boost Praying Skill", "PrayingBoostTooltip", BoosterPower);
		map.Add(ResourcesGame.BoostPraying.ToString(), Praying);
		Mentalizing.effect = new List<SimpleEffect>();
		Mentalizing.effect.Add(new SimpleEffect(GameManager.Instance.SkillManager.Get(SkillManager.SkillNames.Mentalizing).boost, 15.0, 1.0));
		Mentalizing.Init(13, "Boost Mentalizing Skill", "MentalizingBoostTooltip", BoosterPower);
		map.Add(ResourcesGame.BoostMentalizing.ToString(), Mentalizing);
		Trials.effect = new List<SimpleEffect>();
		Trials.effect.Add(new SimpleEffect(GameManager.Instance.Trials.RewardSize, 0.0, 1.2000000476837158));
		Trials.Init(20, "Trials Buff", "[Trial reward] +20%");
		map.Add(ResourcesGame.BuffTrial.ToString(), Trials);
		Catas.effect = new List<SimpleEffect>();
		Catas.effect.Add(new SimpleEffect(GameManager.Instance.BuildingManager.AllIncome, 0.0, 1.2000000476837158));
		Catas.Init(21, "Catalysts Buff", "[Catalyst income] +20%");
		map.Add(ResourcesGame.BuffCats.ToString(), Catas);
		Bats.effect = new List<SimpleEffect>();
		Bats.effect.Add(new SimpleEffect(GameContext.GetResource("Bats.SpawnRate"), 0.0, 2.0));
		Bats.Init(22, "Bats Buff", "[BatsSpawnRate] +100%");
		map.Add(ResourcesGame.BuffBats.ToString(), Bats);
		AutoExpedition.effect = new List<SimpleEffect>();
		AutoExpedition.effect.Add(new SimpleEffect(ExpeditionManager.Instance.WinChanceBonus, 100.0, 1.0));
		AutoExpedition.Init(23, "Expedition Buff", "AutoExpedBuffTooltip");
		map.Add(ResourcesGame.BuffAutoExped.ToString(), AutoExpedition);
	}

	public void Restart()
	{
		foreach (Buff item in GetAll())
		{
			item.SetTime(0f);
		}
		Gathering.SetTime(0f);
		Crafting.SetTime(0f);
		Praying.SetTime(0f);
		Mentalizing.SetTime(0f);
	}

	public int GainRandom(float t, bool ignoreActive = false)
	{
		List<BuffProgressive> list = ((!ignoreActive) ? GetAllNonActive() : GetAllBuffs());
		if (list.Count > 0)
		{
			Buff buff = list[UnityEngine.Random.Range(0, list.Count)];
			if (isAvailable())
			{
				buff.Activate(t);
			}
			else
			{
				buff.AddTime(t);
			}
			return buff.ID;
		}
		return -1;
	}

	public void Give(string key, int value)
	{
		if (map.ContainsKey(key))
		{
			map[key].Activate(value);
		}
	}

	public void GiveProgressive(int id, int value)
	{
		if (progressives.ContainsKey(id))
		{
			progressives[id].Activate(value);
		}
	}

	public int GetBuffId(string key)
	{
		if (map.ContainsKey(key))
		{
			return map[key].ID;
		}
		return -1;
	}

	public void OnRealmChange()
	{
		foreach (BuffProgressive allBuff in GetAllBuffs())
		{
			allBuff.SetTime(0f);
			allBuff.TimeSpent = 0.0;
		}
		OffBuffs();
	}

	public Dictionary<int, double> SaveRetain()
	{
		if (SaveExpPart.ValueFloat == 1f)
		{
			return null;
		}
		Dictionary<int, double> dictionary = new Dictionary<int, double>();
		double num = 0.0;
		foreach (BuffProgressive allBuff in GetAllBuffs())
		{
			num = (allBuff.TimeSpent + (double)allBuff.CurrentTime) * (double)(SaveExpPart.ValueFloat - 1f);
			if (num > 0.0)
			{
				dictionary.Add(allBuff.ID, num);
			}
		}
		if (dictionary.Count == 0)
		{
			return null;
		}
		return dictionary;
	}

	public void LoadRetain(Dictionary<int, double> saved)
	{
		if (saved == null)
		{
			return;
		}
		GetAllBuffs();
		foreach (KeyValuePair<int, double> item in saved)
		{
			if (progressives.ContainsKey(item.Key))
			{
				progressives[item.Key].TimeSpent = item.Value;
			}
		}
	}

	public void OffBuffs()
	{
		foreach (Buff item in GetAll())
		{
			item.OffOnReset();
		}
	}

	public void OnBuffs()
	{
		foreach (Buff item in GetAll())
		{
			item.OnOnReset();
		}
	}

	public List<Buff> GetAll()
	{
		return new List<Buff>
		{
			Profit, Shards, Crit, Void, Attributes, Gathering, Crafting, Praying, Mentalizing, Trials,
			Catas, Bats, AutoExpedition
		};
	}

	public bool BoosterIsActive()
	{
		if (Gathering.CurrentTime <= 0 && Crafting.CurrentTime <= 0 && Praying.CurrentTime <= 0)
		{
			return Mentalizing.CurrentTime > 0;
		}
		return true;
	}

	public List<BuffProgressive> GetAllBuffs()
	{
		List<BuffProgressive> list = new List<BuffProgressive> { Profit, Shards, Crit, Void };
		if (GameManager.Instance.Paragon.AttributesIsAvailable)
		{
			list.Add(Attributes);
		}
		return list;
	}

	public List<BuffProgressive> GetAllActive()
	{
		List<BuffProgressive> list = new List<BuffProgressive>();
		if (Profit.CurrentTime > 0)
		{
			list.Add(Profit);
		}
		if (Shards.CurrentTime > 0)
		{
			list.Add(Shards);
		}
		if (Crit.CurrentTime > 0)
		{
			list.Add(Crit);
		}
		if (Void.CurrentTime > 0)
		{
			list.Add(Void);
		}
		if (Attributes.CurrentTime > 0)
		{
			list.Add(Attributes);
		}
		return list;
	}

	public List<BuffProgressive> GetAllNonActive()
	{
		List<BuffProgressive> list = new List<BuffProgressive>();
		if (Profit.CurrentTime <= 0)
		{
			list.Add(Profit);
		}
		if (Shards.CurrentTime <= 0)
		{
			list.Add(Shards);
		}
		if (Crit.CurrentTime <= 0)
		{
			list.Add(Crit);
		}
		if (Void.CurrentTime <= 0)
		{
			list.Add(Void);
		}
		if (Attributes.CurrentTime <= 0)
		{
			list.Add(Attributes);
		}
		return list;
	}

	public bool isAvailable()
	{
		return true;
	}

	public void Spread(int id, int seconds)
	{
		List<BuffProgressive> list = new List<BuffProgressive>();
		foreach (KeyValuePair<int, BuffProgressive> progressife in progressives)
		{
			if (progressife.Key != id && progressife.Value.CurrentTime > 0)
			{
				list.Add(progressife.Value);
			}
		}
		if (list.Count == 0)
		{
			progressives[id].AddTime(seconds);
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			list[i].AddTime(seconds / list.Count);
		}
		int num = seconds % list.Count;
		if (num > 0)
		{
			list[0].AddTime(num);
		}
	}

	public BuffSave Save()
	{
		BuffSave buffSave = new BuffSave();
		buffSave.progression = new Dictionary<int, double>();
		foreach (KeyValuePair<int, BuffProgressive> progressife in progressives)
		{
			if (progressife.Value.TimeSpent > 0.0)
			{
				buffSave.progression.Add(progressife.Value.ID, progressife.Value.TimeSpent);
			}
		}
		buffSave.timers = new Dictionary<int, float>();
		foreach (KeyValuePair<string, Buff> item in map)
		{
			if (item.Value.Active)
			{
				buffSave.timers.Add(item.Value.ID, item.Value.CurrentTime);
			}
		}
		return buffSave;
	}

	public void Load(BuffSave save)
	{
		foreach (KeyValuePair<int, BuffProgressive> progressife in progressives)
		{
			progressife.Value.TimeSpent = 0.0;
		}
		foreach (KeyValuePair<string, Buff> item in map)
		{
			item.Value.SetTime(0f);
		}
		if (save == null)
		{
			return;
		}
		foreach (KeyValuePair<int, double> item2 in save.progression)
		{
			progressives[item2.Key].TimeSpent = item2.Value;
		}
		List<Buff> all = GetAll();
		foreach (KeyValuePair<int, float> v in save.timers)
		{
			Buff buff = all.Find((Buff x) => x.ID == v.Key);
			if (buff != null)
			{
				buff.SetTime(v.Value);
			}
		}
	}
}
