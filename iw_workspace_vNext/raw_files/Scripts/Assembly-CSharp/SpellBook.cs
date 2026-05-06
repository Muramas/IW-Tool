using System;
using System.Collections;
using System.Collections.Generic;
using Planes;
using UnityEngine;

public class SpellBook : MonoBehaviour
{
	public SpriteAtlas atlas;

	public Transform SpellContent;

	public List<Spell> AvailableSpells;

	public List<Spell> SpellList;

	public List<Spell> PermanentSpellList;

	public List<Spell> AccumalatedSpellList;

	public SetsPanel SetsPanel;

	private Dictionary<Spells, List<IEffect>> effect_map;

	private Dictionary<Spells, Action> on_choose;

	private Dictionary<Spells, Action> on_clear;

	private List<Spells> offline;

	public Dictionary<Spells, IEffect> persistentPassive;

	public List<Spells> MemeticBanList;

	public List<Spells> NonscaledSpells;

	[SerializeField]
	private SpellChoose prefab;

	public List<SpellChoose> SpellChooses;

	public EnhancementController Enhancements;

	public void Init()
	{
		SpellChooses = new List<SpellChoose>();
		AvailableSpells = new List<Spell>();
		SpellList = new List<Spell>();
		PermanentSpellList = new List<Spell>();
		AccumalatedSpellList = new List<Spell>();
		effect_map = new Dictionary<Spells, List<IEffect>>();
		on_choose = new Dictionary<Spells, Action>();
		on_clear = new Dictionary<Spells, Action>();
		offline = new List<Spells>();
		MemeticBanList = new List<Spells>();
		NonscaledSpells = new List<Spells>();
		Enhancements = new EnhancementController();
		create_all();
		InitPermanent();
		SetsPanel.Init();
		Enhancements.MarkEnhance();
	}

	public void ChangeSpellSet()
	{
		set_available_spells();
		InitSpellChooses();
		UpdateSpellChooses();
	}

	public void Offline(BigNumber t)
	{
		CalculateOffline(t);
	}

	public List<Spell> GetAllPersistentSpells()
	{
		List<Spell> list = SpellList.FindAll((Spell x) => x.IsPersistent);
		List<Spells> list2 = new List<Spells>();
		foreach (Spell item2 in list)
		{
			if (item2.NameKey == Spells.HCGain && (!GameManager.Instance.Realmcraft.IsActive || GameManager.Instance.Realmcraft.Active.ID != "ironsoul"))
			{
				continue;
			}
			if (!GameManager.Instance.Realmcraft.IsActive || GameManager.Instance.Realmcraft.Active.ID != "arcan")
			{
				if (item2.NameKey == Spells.JAMissileStorm2)
				{
					continue;
				}
			}
			else if (item2.NameKey == Spells.JAMissileStorm)
			{
				continue;
			}
			if (!item2.IsEnhancement || Enhancements.IsUnlocked(item2.NameKey))
			{
				Spells item = Enhancements.Upgrade(item2.NameKey);
				if (!list2.Contains(item))
				{
					list2.Add(item);
				}
			}
		}
		List<Spell> list3 = new List<Spell>();
		foreach (Spells item3 in list2)
		{
			list3.Add(GetSpell(item3));
		}
		return list3;
	}

	private IEnumerator wait(int frames, Action action)
	{
		for (int i = 0; i < frames; i++)
		{
			yield return null;
		}
		action?.Invoke();
	}

	private void CalculateOffline(BigNumber t)
	{
		BigNumber bigNumber = 0.0;
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		BigNumber change = 0.0;
		BigNumber bigNumber2 = 0.0;
		BigNumber bigNumber3 = 0.0;
		Dictionary<Spells, BigNumber> offline_casts = new Dictionary<Spells, BigNumber>();
		foreach (Spell availableSpell in AvailableSpells)
		{
			BigNumber bigNumber4 = availableSpell.useCounter.GetValue();
			BigNumber bigNumber5 = 0.0;
			bigNumber = 0.0;
			if (bigNumber4 > 0.0)
			{
				availableSpell.Use.ChangeValue(bigNumber4 * t, 1.0);
				bigNumber5 = bigNumber4 * t;
			}
			bigNumber4 = availableSpell.useThisRunCounter.GetValue();
			if (bigNumber4 > 0.0)
			{
				bigNumber = bigNumber4 * t;
				availableSpell.UseThisRun.Change(bigNumber);
				if (bigNumber > bigNumber5)
				{
					bigNumber5 = bigNumber;
				}
				if (availableSpell.IsWorkOffline)
				{
					offline_casts.Add(availableSpell.NameKey, bigNumber);
				}
			}
			if (bigNumber5 > 0.0)
			{
				change += bigNumber5;
				if (availableSpell.IsAccumulated || availableSpell.IsPersistent)
				{
					bigNumber2 += bigNumber5;
				}
				if (availableSpell.Type == SpellTypeGroup.Evocation)
				{
					bigNumber3 += bigNumber5;
				}
				scrolls.OnCastAmount?.Invoke(availableSpell, bigNumber5.ToDouble());
			}
			if (bigNumber > 0.0)
			{
				scrolls.OnCastOffline?.Invoke(availableSpell, bigNumber.ToDouble());
			}
		}
		if (bigNumber2 > 1.0)
		{
			scrolls.AccumCastCount.Change(bigNumber2);
		}
		if (bigNumber3 > 1.0)
		{
			scrolls.EvoCastCount.Change(bigNumber3);
		}
		Statistic.Change(Statistic.CastSpell, change);
		Statistic.Change(Statistic.CastSpellTotal, change);
		Statistic.Change(Statistic.CastSpellRealm, change);
		if (offline_casts.Count > 0)
		{
			StartCoroutine(wait(2, delegate
			{
				OfflineSpell(offline_casts);
			}));
		}
	}

	private void OfflineSpell(Dictionary<Spells, BigNumber> offline_casts)
	{
		foreach (KeyValuePair<Spells, BigNumber> offline_cast in offline_casts)
		{
			foreach (IEffect effect in GetSpell(offline_cast.Key).effects)
			{
				if (effect is IOfflineEffect)
				{
					(effect as IOfflineEffect).Offline(offline_cast.Value);
				}
			}
		}
	}

	private void InitSpellChooses()
	{
		AvailableSpells.Sort((Spell x, Spell y) => (x.level_req >= y.level_req) ? 1 : (-1));
		int count = AvailableSpells.Count;
		for (int num = 0; num < count; num++)
		{
			if (SpellChooses.Count < num + 1)
			{
				SpellChooses.Add(UnityEngine.Object.Instantiate(prefab, SpellContent));
			}
			SpellChooses[num].Init(AvailableSpells[num]);
		}
		for (int num2 = count; num2 < SpellChooses.Count; num2++)
		{
			SpellChooses[num2].gameObject.SetActive(value: false);
			SpellChooses[num2].Init(null);
		}
	}

	public void PostLoad()
	{
		foreach (Spell permanentSpell in PermanentSpellList)
		{
			if (permanentSpell.Use.Value.Mantissa != 0.0)
			{
				permanentSpell.Apply();
			}
		}
		BigNumber value = 0.0;
		foreach (Spell spell in SpellList)
		{
			if (spell.Type == SpellTypeGroup.Evocation && spell.Use.Value >= 1.0)
			{
				value += spell.Use.Value;
			}
		}
		GameManager.Instance.Scrolls.EvoCastCount.SetValue(value);
		if (!(GameManager.Instance.Scrolls.AccumCastCount.Value < 1.0))
		{
			return;
		}
		foreach (Spell spell2 in SpellList)
		{
			if (spell2.IsAccumulated || spell2.IsPersistent)
			{
				value += spell2.UseThisRun.Value;
			}
		}
		GameManager.Instance.Scrolls.AccumCastCount.SetValue(value);
	}

	private void set_available_spells()
	{
		List<Spells> list = GameManager.Instance.CurrentHero.Hero.SpellList;
		if (GameManager.Instance.ChallengeManager.ActiveChallenge == null && !Settings.DisableEnhancement)
		{
			list = Enhancements.Replace(list);
		}
		AvailableSpells = new List<Spell>();
		foreach (Spells s in list)
		{
			Spell spell = SpellList.Find((Spell x) => x.NameKey == s);
			if (spell != null)
			{
				AvailableSpells.Add(spell);
			}
		}
		foreach (SpellChoose spellChoose in SpellChooses)
		{
			if (!AvailableSpells.Contains(spellChoose.Spell))
			{
				spellChoose.gameObject.SetActive(value: false);
				GameManager.Instance.Scrolls.ClearSlot(spellChoose.Spell);
			}
			else
			{
				spellChoose.gameObject.SetActive(value: true);
			}
		}
	}

	public void AddSpell(Spell sp)
	{
		if (!AvailableSpells.Contains(sp))
		{
			AvailableSpells.Add(sp);
			InitSpellChooses();
			UpdateSpellChooses();
		}
	}

	private void UpdateSpellChooses()
	{
		foreach (SpellChoose spellChoose in SpellChooses)
		{
			if (spellChoose.Spell == null)
			{
				spellChoose.gameObject.SetActive(value: false);
			}
			else if (!AvailableSpells.Contains(spellChoose.Spell))
			{
				spellChoose.gameObject.SetActive(value: false);
				GameManager.Instance.Scrolls.ClearSlot(spellChoose.Spell);
			}
			else
			{
				spellChoose.gameObject.SetActive(value: true);
			}
		}
	}

	public Spell GetSpell(Spells key)
	{
		return SpellList.Find((Spell x) => x.NameKey == key);
	}

	public Spell GetActualSpell(Spells key)
	{
		if (GameManager.Instance.ChallengeManager.ActiveChallenge != null)
		{
			return GetSpell(key);
		}
		return GetSpell(Enhancements.Upgrade(key));
	}

	public void ResetAll(bool resetAll = false)
	{
		GetSpell(Spells.RitualOfPotency).ResetUsesAll(resetAll);
	}

	private void InitPermanent()
	{
		VariableComplex incantationEfficiency = GameManager.Instance.Scrolls.IncantationEfficiency;
		incantationEfficiency.OnChange = (Action)Delegate.Combine(incantationEfficiency.OnChange, (Action)delegate
		{
			foreach (Spell permanentSpell in PermanentSpellList)
			{
				if (permanentSpell.NameKey != Spells.QuasiIncantation && permanentSpell.UseThisRun.Value > 1.0 && permanentSpell.Type == SpellTypeGroup.Incantation)
				{
					permanentSpell.ForceUpdate();
				}
			}
		});
		VariableComplex summoningEfficiency = GameManager.Instance.Scrolls.SummoningEfficiency;
		summoningEfficiency.OnChange = (Action)Delegate.Combine(summoningEfficiency.OnChange, (Action)delegate
		{
			foreach (Spell permanentSpell2 in PermanentSpellList)
			{
				if (permanentSpell2.Type == SpellTypeGroup.Summoning && permanentSpell2.UseThisRun.Value > 1.0)
				{
					permanentSpell2.ForceUpdate();
				}
			}
		});
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, (Action<Spell>)delegate(Spell s)
		{
			if (s.Type == SpellTypeGroup.Incantation && s.Duration.Value != 0.0)
			{
				GetSpell(Spells.NotAI).Update();
			}
		});
		ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
		scrolls2.OnPostCast = (Action<Spell>)Delegate.Combine(scrolls2.OnPostCast, (Action<Spell>)delegate(Spell s)
		{
			if (s.Type == SpellTypeGroup.Incantation && s.Duration.Value != 0.0)
			{
				GetSpell(Spells.NotAI).Update();
			}
		});
		foreach (Spell permanentSpell3 in PermanentSpellList)
		{
			VariableComplex use = permanentSpell3.Use;
			use.OnChange = (Action)Delegate.Combine(use.OnChange, new Action(permanentSpell3.ForceUpdate));
		}
	}

	private void create_all()
	{
		foreach (SpellFormat spell in GlobalData.Spells)
		{
			Spell item = new Spell(spell);
			SpellList.Add(item);
		}
		create_effect_map();
		foreach (Spell spell2 in SpellList)
		{
			effect_map.TryGetValue(spell2.NameKey, out spell2.effects);
			spell2.SetEfficiency();
			on_choose.TryGetValue(spell2.NameKey, out spell2.OnChoose);
			on_clear.TryGetValue(spell2.NameKey, out spell2.OnClear);
			spell2.IsWorkOffline = offline.Contains(spell2.NameKey);
			if (spell2.IsAccumulated)
			{
				AccumalatedSpellList.Add(spell2);
			}
			if (spell2.IsPersistent)
			{
				spell2.PassiveEffect = persistentPassive[spell2.NameKey];
			}
		}
	}

	private void create_effect_map()
	{
		List<IEffect> list = new List<IEffect>();
		EffectAddTemporaryBuildings item = new EffectAddTemporaryBuildings(1, 100, 250.0, -3.5, 1.0, Reborn.Souls);
		list.Add(item);
		effect_map.Add(Spells.GemResonance, list);
		list = new List<IEffect>();
		list.Add(new EffectAddProduction(30f, 0.0, 0.0));
		effect_map.Add(Spells.MagicMissile, list);
		list = new List<IEffect>();
		List<Spells> list2 = new List<Spells>();
		list2.Add(Spells.SpellFocus);
		EffectAddShardInstant effectAddShardInstant = new EffectAddShardInstant(25.0, 1.25, 0.0, GameManager.Instance.CurrentHero.Hero.Level, list2);
		effectAddShardInstant.pow_diminishing = 0.075f;
		list.Add(effectAddShardInstant);
		effect_map.Add(Spells.SpellFocus, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(5f, 1f, -1f, 0f, crit_const: true));
		SimpleEffect simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.ShardsPool.Capacity);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 9.999999747378752E-05;
		simpleEffect.mult = 0.44999998807907104;
		simpleEffect.parameter = GetSpell(Spells.ConjureLesserElemental).Use;
		simpleEffect.diminishing = 0.8f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ConjureLesserElemental, list);
		list = new List<IEffect>();
		EffectAutoClick item2 = new EffectAutoClick(10f, 1f, 5f, 7.5f, crit_const: true);
		list.Add(item2);
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.SpellChargingSpeed);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 0.0010000000474974513;
		simpleEffect.mult = 0.3499999940395355;
		simpleEffect.diminishing = 0.5f;
		simpleEffect.parameter = GetSpell(Spells.ConjureGreaterElemental).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ConjureGreaterElemental, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(15f, 1f, 10f, 12f, crit_const: true);
		list.Add(item2);
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentPet.ExpBonus);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 0.009999999776482582;
		simpleEffect.mult = 0.5;
		simpleEffect.diminishing = 0.95f;
		simpleEffect.parameter = GetSpell(Spells.ConjurePrimalElemental).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ConjurePrimalElemental, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed, 0.0, 2.0, EffectNames.Linear, 0.9f);
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.BonusLifeTime, 0.0, 2.0, EffectNames.Linear, 0.5f);
		list.Add(simpleEffect);
		effect_map.Add(Spells.VoidLure, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.Orb.click_profit, 0.0, 2.0));
		simpleEffect = new SimpleEffect(GameManager.Instance.Orb.crit_chance, 3.0, 1.0);
		simpleEffect.diminishing = 0.9f;
		list.Add(simpleEffect);
		list.Add(new SimpleEffect(GameManager.Instance.Orb.crit_profit, 0.0, 1.5));
		effect_map.Add(Spells.MagicalWeapon, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect(EffectNames.Pow.ToString());
		simpleEffect.target = GameManager.Instance.Profit;
		simpleEffect.add = 0.10000000149011612;
		simpleEffect.mult = 0.3400000035762787;
		simpleEffect.parameter = Statistic.TotalBuildings;
		list.Add(simpleEffect);
		effect_map.Add(Spells.Empower, list);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Profit, 0.8999999761581421, 0.44999998807907104, GetSpell(Spells.RitualOfPower).Use, EffectNames.Pow)
		};
		effect_map.Add(Spells.RitualOfPower, list);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Profit, 0.5, 0.800000011920929, GetSpell(Spells.RitualOfPotency).UseThisRun, EffectNames.PowA)
		};
		effect_map.Add(Spells.RitualOfPotency, list);
		list = new List<IEffect>();
		EffectAutoVoidCollect item3 = new EffectAutoVoidCollect(40f, Statistic.PetMaxLevel, GetSpell(Spells.VoidAutomaton).Use);
		list.Add(item3);
		effect_map.Add(Spells.VoidAutomaton, list);
		MemeticBanList.Add(Spells.VoidAutomaton);
		list = new List<IEffect>();
		list.Add(new SEEffect());
		effect_map.Add(Spells.SyntheticEntity, list);
		Action<float> synt_entity_tick = delegate(float k)
		{
			Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == Spells.SyntheticEntity);
			if (!(scroll == null))
			{
				int num = BigNumber.AmountOfElementsAriphmeticProgression(k * GameManager.Instance.Scrolls.SpellChargingSpeed.Value.ToFloat(), scroll.spell.FullBuild / 2.0, scroll.spell.FullBuild * (double)(1f + (float)scroll.spell.chargeCount) / 2.0).ToInt();
				if (num > 0)
				{
					k = 0f;
					scroll.spell.chargeCount += (ulong)num;
					if (scroll.spell.chargeCount > 1000000000)
					{
						scroll.spell.chargeCount = 1000000000uL;
					}
				}
				else
				{
					k = k * 2f / (1f + (float)scroll.spell.chargeCount);
					scroll.AddProgressBuild(k, text: false, shards: false);
				}
			}
		};
		Action value = delegate
		{
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Combine(instance.GameTick, synt_entity_tick);
		};
		on_choose.Add(Spells.SyntheticEntity, value);
		Action value2 = delegate
		{
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Remove(instance.GameTick, synt_entity_tick);
		};
		on_clear.Add(Spells.SyntheticEntity, value2);
		list = new List<IEffect>();
		list.Add(new EffectVR());
		effect_map.Add(Spells.VoidRadiance, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(16f, 1f, 10f, 9f, crit_const: true);
		list.Add(item2);
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.ShardsPassive;
		simpleEffect.mult = 2.0;
		simpleEffect.pow_diminishing = 0.2f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ConjureManabeast, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(9f));
		simpleEffect = new SimpleEffect(GameManager.Instance.Orb.critRating.crit_rating);
		simpleEffect.add = 0.0010000000474974513;
		simpleEffect.mult = 0.0;
		simpleEffect.pow_diminishing = 0.9f;
		simpleEffect.parameter = Statistic.AutoClicks;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SummonWoodlandCreatures, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(12f);
		list.Add(item2);
		simpleEffect = new SimpleEffect(GameManager.Instance.Orb.crit_chance);
		simpleEffect.add = 20.0;
		simpleEffect.diminishing = 0.075f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SummonGuardianOfTheCanopies, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameContext.GetResource("Building.4.Profit"));
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.add = 1.0;
		simpleEffect.mult = 0.5;
		simpleEffect.pow_diminishing = 1.2f;
		simpleEffect.parameter = Statistic.AutoClicks;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ForceOfNature, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Orb.autoclick_profit);
		simpleEffect.add = 0.0;
		simpleEffect.mult = 0.20000000298023224;
		simpleEffect.parameter = GetSpell(Spells.RulesOfNature).UseThisRun;
		list.Add(simpleEffect);
		effect_map.Add(Spells.RulesOfNature, list);
		Action<float> rules_of_nature_crit = delegate(float amount)
		{
			GameManager.Instance.Scrolls.FillScroll(Spells.RulesOfNature, amount);
		};
		value = delegate
		{
			Orb orb = GameManager.Instance.Orb;
			orb.OnCrit = (Action<float>)Delegate.Combine(orb.OnCrit, rules_of_nature_crit);
		};
		on_choose.Add(Spells.RulesOfNature, value);
		value2 = delegate
		{
			Orb orb = GameManager.Instance.Orb;
			orb.OnCrit = (Action<float>)Delegate.Remove(orb.OnCrit, rules_of_nature_crit);
		};
		on_clear.Add(Spells.RulesOfNature, value2);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(0.5f, 10f);
		list.Add(item2);
		CombineEffect combineEffect = new CombineEffect(GameManager.Instance.Orb.autoclick_profit);
		combineEffect.AddEffect(0.0, 1.5, GameManager.Instance.CurrentHero.Level, GameContext.GetEffect(EffectNames.Linear.ToString()), eff: true, 1f, 0.9f);
		combineEffect.AddEffect(0.0, 0.10000000149011612, GetSpell(Spells.DeepwoodStalker).Use, GameContext.GetEffect(EffectNames.Linear.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.DeepwoodStalker, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Profit);
		simpleEffect.effect = GameContext.GetEffect(EffectNames.PowA.ToString());
		simpleEffect.add = 1.0;
		simpleEffect.mult = 0.7400000095367432;
		simpleEffect.pow_diminishing = 0.9f;
		simpleEffect.parameter = GameManager.Instance.BuildingManager.GetBuilding(4).TotalLevel;
		list.Add(simpleEffect);
		list.Add(new EffectAutoclickVariable(0f, 0.004f, GameManager.Instance.BuildingManager.GetBuilding(4).TotalLevel, new EffectAutoClick(1f, 1f, 1f, 1f, crit_const: false, 1f, fromSpell: false), useLog: false, fromSpell: true));
		effect_map.Add(Spells.SummonEvergrowingForest, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoclickVariable(1f, 2.5f, GameManager.Instance.Scrolls.SummoningEfficiency, new EffectAutoClick(1f, 1f, 1f, 1f, crit_const: false, 1f, fromSpell: false), useLog: true, fromSpell: true));
		simpleEffect = new SimpleEffect(GameManager.Instance.Orb.percent_pps);
		simpleEffect.add = 0.0;
		simpleEffect.mult = 9.999999747378752E-05;
		simpleEffect.parameter = Statistic.AutoClicks;
		simpleEffect.pow_diminishing = 0.9f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SummonCentipedeSwarm, list);
		list = new List<IEffect>();
		list.Add(new EffectAddProduction(30f, 0.0, 0.0));
		item2 = new EffectAutoClick(4f);
		item2.diminishing = 0.01f;
		item2.PetClick = true;
		list.Add(item2);
		effect_map.Add(Spells.FireBall, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(8f);
		item2.PetClick = true;
		list.Add(item2);
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentPet.ExpBonus, 1.0, 0.4000000059604645, EffectNames.Pow);
		simpleEffect.diminishing = 0.5f;
		simpleEffect.parameter = GetSpell(Spells.SummonInfernalThrasher).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SummonInfernalThrasher, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(14f);
		item2.PetClick = true;
		list.Add(item2);
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 0.0010000000474974513, 1.100000023841858, EffectNames.Pow);
		simpleEffect.parameter = GetSpell(Spells.SummonHornedIncinerator).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SummonHornedIncinerator, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.target = GameContext.GetResource("Building.6.Profit");
		simpleEffect.add = 0.25;
		simpleEffect.mult = 1.25;
		simpleEffect.pow_diminishing = 1.6f;
		simpleEffect.parameter = GameManager.Instance.CurrentPet.Level;
		list.Add(simpleEffect);
		effect_map.Add(Spells.GobletOfFire, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameManager.Instance.CurrentPet.AbilityPower;
		simpleEffect.add = 0.800000011920929;
		simpleEffect.mult = 0.27;
		simpleEffect.parameter = GameManager.Instance.BuildingManager.GetBuilding(6).TotalLevel;
		list.Add(simpleEffect);
		effect_map.Add(Spells.Hellrage, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameManager.Instance.CurrentPet.ExpBonus;
		simpleEffect.add = 0.550000011920929;
		simpleEffect.mult = 0.4099999964237213;
		simpleEffect.parameter = GetSpell(Spells.UncleanKnowledge).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.UncleanKnowledge, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameManager.Instance.Profit;
		simpleEffect.add = 0.1599999964237213;
		simpleEffect.mult = 0.46000000834465027;
		EffectReapPetExp item4 = new EffectReapPetExp(simpleEffect, 0.4f);
		list.Add(item4);
		effect_map.Add(Spells.ReapWhatYouSow, list);
		list = new List<IEffect>();
		list.Add(new EffectAddProductionPeriodic(10f, 1f, 0.0, 0.009999999776482582, GetSpell(Spells.HellStorm).Use));
		ActionEffect item5 = new ActionEffect(null, null, delegate(BigNumber eff)
		{
			if (GameManager.Instance.CurrentPet.Pet != null)
			{
				GameManager.Instance.CurrentPet.Pet.AddExp(GameManager.Instance.CurrentHero.Level.ValueInt * (1.0 + eff.Pow(0.5)));
			}
		}, 1f);
		list.Add(item5);
		effect_map.Add(Spells.HellStorm, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(6f, 1f, 6f, 2f, crit_const: true);
		list.Add(item2);
		simpleEffect = new SimpleEffect(GameManager.Instance.Profit);
		simpleEffect.mult = 1.5;
		simpleEffect.pow_diminishing = 0.9f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.AnimateSkeletalMinions, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(16f, 1f, 15f, 4f, crit_const: true);
		list.Add(item2);
		simpleEffect = new SimpleEffect(GameManager.Instance.Idle.IdleBonus);
		simpleEffect.mult = 1.5;
		simpleEffect.pow_diminishing = 0.8f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SummonUnholyAvatar, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(18f, 1f, 18f, 5f, crit_const: true);
		list.Add(item2);
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower);
		simpleEffect.mult = 1.5;
		simpleEffect.pow_diminishing = 0.9f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.AnimateFesteringAbomination, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameContext.GetResource("Building.2.Profit"));
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.add = 0.8500000238418579;
		simpleEffect.mult = 0.4300000071525574;
		simpleEffect.pow_diminishing = 0.92f;
		simpleEffect.parameter = GameManager.Instance.Idle.IdleBonus;
		list.Add(simpleEffect);
		effect_map.Add(Spells.DreadedScriptOfHarvest, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameManager.Instance.Profit;
		simpleEffect.add = 0.75;
		simpleEffect.mult = 0.8500000238418579;
		simpleEffect.parameter = GetSpell(Spells.Nightfall).UseThisRun;
		list.Add(simpleEffect);
		effect_map.Add(Spells.Nightfall, list);
		Action<Spell> nightfall_oncast = delegate(Spell s)
		{
			if (s.NameKey != Spells.Nightfall)
			{
				GameManager.Instance.Scrolls.FillScroll(Spells.Nightfall, 1.0);
			}
		};
		Action<float> nightfall = delegate(float t)
		{
			float num = Mathf.Clamp(((GameManager.Instance.CurrentHero.PlayedTime.ValueInt + GameManager.Instance.CurrentHero.SkipedPlayedTime.Value) / 3600.0).ToFloat(), 1f, 150f);
			num /= 5f;
			GameManager.Instance.Scrolls.FillScroll(Spells.Nightfall, t * num);
		};
		value = delegate
		{
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Combine(instance.GameTick, nightfall);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, nightfall_oncast);
		};
		on_choose.Add(Spells.Nightfall, value);
		value2 = delegate
		{
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Remove(instance.GameTick, nightfall);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, nightfall_oncast);
		};
		on_clear.Add(Spells.Nightfall, value2);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.EvocationEfficiency;
		simpleEffect.mult = 0.02800000086426735;
		simpleEffect.parameter = GameManager.Instance.CurrentHero.Level;
		simpleEffect.pow_diminishing = 1.2f;
		list.Add(simpleEffect);
		item5 = new ActionEffect(null, null, delegate
		{
			if (GameManager.Instance.CurrentHero.Hero.SpellList.Contains(Spells.PlagueZombie))
			{
				Spell spell = GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.PlagueZombie);
				float value3 = 200f / (float)(spell.chargeCount + 1);
				value3 = Mathf.Clamp(value3, 0f, 100f) / 100f * UnityEngine.Random.Range(0.5f, 1.5f);
				spell.AddProgressBuild(value3, shards: false);
			}
		}, 1f);
		list.Add(item5);
		effect_map.Add(Spells.VoraciousPlague, list);
		list = new List<IEffect>();
		list.Add(new MultiChargesEffect(20f, Spells.PlagueZombie, 0.15f));
		effect_map.Add(Spells.PlagueZombie, list);
		list = new List<IEffect>();
		list.Add(new EffectAddProduction(40f, 0.0, 0.0));
		List<Spells> list3 = new List<Spells>();
		list3.Add(Spells.LightningBolt);
		effectAddShardInstant = new EffectAddShardInstant(100.0, 4.0, 0.0, GameManager.Instance.CurrentHero.Hero.Level, list3);
		effectAddShardInstant.pow_diminishing = 0.075f;
		list.Add(effectAddShardInstant);
		effect_map.Add(Spells.LightningBolt, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.ShardsPerClick);
		simpleEffect.mult = 3.0;
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency);
		simpleEffect.mult = 3.0;
		simpleEffect.pow_diminishing = 0.8f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SpellStaffOfChamaon, list);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Profit);
		combineEffect.AddEffect(1.0, 0.44999998807907104, Statistic.TotalBuildings, GameContext.GetEffect(EffectNames.Pow.ToString()));
		combineEffect.AddEffect(0.007499999832361937, 0.25, Statistic.CastSpell, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.KarnaphensSpellshroud, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameContext.GetResource("Building.3.Profit");
		simpleEffect.add = 2.1500000953674316;
		simpleEffect.mult = 0.8100000023841858;
		simpleEffect.parameter = GetSpell(Spells.RadiantPools).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.RadiantPools, list);
		list = new List<IEffect>();
		EffectUseMM effectUseMM = new EffectUseMM();
		effectUseMM.base_count = 10;
		effectUseMM.add = 1000.0;
		effectUseMM.mult = 1.3f;
		effectUseMM.parameter = GetSpell(Spells.JAMissileStorm).UseThisRun;
		list.Add(effectUseMM);
		effect_map.Add(Spells.JAMissileStorm, list);
		NonscaledSpells.Add(Spells.JAMissileStorm);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower);
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.add = 0.20000000298023224;
		simpleEffect.mult = 1.149999976158142;
		simpleEffect.parameter = GetSpell(Spells.ArcaneInfusion).Use;
		simpleEffect.diminishing = 2f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ArcaneInfusion, list);
		list = new List<IEffect>();
		list.Add(new EffectAddProduction(10f, 4.0, 0.0, GameManager.Instance.CurrentHero.Hero.Level));
		effect_map.Add(Spells.KelphiorsBlackBeam, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect(EffectNames.Pow.ToString());
		simpleEffect.target = GameManager.Instance.Profit;
		simpleEffect.add = 0.5;
		simpleEffect.mult = 0.44999998807907104;
		simpleEffect.parameter = Statistic.TotalBuildings;
		list.Add(simpleEffect);
		effect_map.Add(Spells.AlterTheLaws, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.target = GameManager.Instance.Profit;
		simpleEffect.add = 1.0;
		simpleEffect.mult = 0.8500000238418579;
		simpleEffect.parameter = GetSpell(Spells.TrueSorcery).UseThisRun;
		list.Add(simpleEffect);
		effect_map.Add(Spells.TrueSorcery, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameManager.Instance.Scrolls.EvocationEfficiency;
		simpleEffect.add = 0.25;
		simpleEffect.mult = 1.2799999713897705;
		simpleEffect.parameter = GameManager.Instance.CurrentHero.Level;
		list.Add(simpleEffect);
		effect_map.Add(Spells.PrimalPower, list);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Profit);
		combineEffect.AddEffect(0.0, 0.000750000006519258, GameManager.Instance.BuildingManager.GetBuilding(8).TotalLevel, GameContext.GetEffect(EffectNames.Linear.ToString()));
		combineEffect.AddEffect(0.10000000149011612, 0.75, GetSpell(Spells.LeyOverdrive).Use, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false, 0.8f);
		list.Add(combineEffect);
		effect_map.Add(Spells.LeyOverdrive, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.IncantationEfficiency);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 0.0025;
		simpleEffect.mult = 1.5;
		simpleEffect.parameter = GetSpell(Spells.QuasiIncantation).Use;
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.QuasiIncantation, list);
		Action<float> q_incantation_tick = delegate(float k)
		{
			Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == Spells.QuasiIncantation);
			if (!(scroll == null))
			{
				float num = 421f - (float)GameManager.Instance.CurrentHero.Level.ValueInt * 2f;
				if (num < 1f)
				{
					num = 1f;
				}
				k = k * 1000f / num;
				scroll.AddProgressBuild(k, text: false, shards: false);
			}
		};
		value = delegate
		{
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Combine(instance.GameTick, q_incantation_tick);
		};
		on_choose.Add(Spells.QuasiIncantation, value);
		value2 = delegate
		{
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Remove(instance.GameTick, q_incantation_tick);
		};
		on_clear.Add(Spells.QuasiIncantation, value2);
		NonscaledSpells.Add(Spells.QuasiIncantation);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower, 0.0, 4.0, GameManager.Instance.CurrentHero.Level);
		simpleEffect.diminishing = 2f;
		list.Add(simpleEffect);
		list.Add(new EffectAddProduction(30f, 1.0, 0.0, GameManager.Instance.CurrentHero.Level, 0f, 1f, refreshPps: true));
		effect_map.Add(Spells.Voidbolt, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoclickVariable(1f, 1f, Statistic.MaxVoidManaSession, new EffectAutoClick(1f, 3f, 1f, 1f, crit_const: false, 1f, fromSpell: false), useLog: true, fromSpell: true));
		list.Add(new EffectAddCollectItem(1, 1f));
		effect_map.Add(Spells.SummonWingedNight, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed, 0.0, 2.0);
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.BonusLifeTime, 0.0, 10.0);
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 0.0, 3.0, EffectNames.Linear, 1.2f);
		list.Add(simpleEffect);
		effect_map.Add(Spells.VoidPrison, list);
		list = new List<IEffect>();
		list.Add(new EffectAddCollectItem(5));
		effect_map.Add(Spells.VoidSyphon, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect(EffectNames.PowA.ToString());
		simpleEffect.target = GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity;
		simpleEffect.add = 0.23000000417232513;
		simpleEffect.mult = 0.44999998807907104;
		simpleEffect.parameter = Statistic.AutoClicks;
		list.Add(simpleEffect);
		effect_map.Add(Spells.EbonTruncheon, list);
		list = new List<IEffect>();
		new List<Spells>().Add(Spells.RealityWarping);
		TradeVoidToShardsEffect item6 = new TradeVoidToShardsEffect(0.83f, 0.26f);
		list.Add(item6);
		effect_map.Add(Spells.RealityWarping, list);
		MemeticBanList.Add(Spells.RealityWarping);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(9f));
		combineEffect = new CombineEffect(GameManager.Instance.VoidManaManager.Power);
		combineEffect.AddEffect(1.0, 0.4000000059604645, Statistic.MaxVoidManaSession, GameContext.GetEffect(EffectNames.PowA.ToString()), eff: true, 1f, 0.65f);
		combineEffect.AddEffect(0.0, 0.10000000149011612, GetSpell(Spells.VoidElemental).Use, GameContext.GetEffect(EffectNames.Linear.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.VoidElemental, list);
		list = new List<IEffect>();
		list.Add(new EffectUseMega(2f, 0.1f));
		effect_map.Add(Spells.Smite, list);
		offline.Add(Spells.Smite);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.Orb.click_profit, 0.0, 3.0));
		simpleEffect = new SimpleEffect(GameManager.Instance.Orb.crit_chance, 5.0, 1.0);
		simpleEffect.diminishing = 0.2f;
		list.Add(simpleEffect);
		list.Add(new SimpleEffect(GameManager.Instance.Orb.crit_profit, 0.0, 2.0));
		effect_map.Add(Spells.SwordOfAnklah, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(15f);
		list.Add(item2);
		effect_map.Add(Spells.TemplarBrothers, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(5f));
		Exorcist exorcist = GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.Hero.NameKey == HeroesNames.Exorcist).Hero as Exorcist;
		Action<float> onClickDA = delegate(float x)
		{
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			if ((nameKey == HeroesNames.Exorcist || nameKey == HeroesNames.Heretic) && 4f >= UnityEngine.Random.Range(0f, 100f))
			{
				exorcist.mc.megaCharges.Change(x);
			}
		};
		item5 = new ActionEffect(delegate
		{
			Orb orb = GameManager.Instance.Orb;
			orb.OnAutoClick = (Action<float>)Delegate.Combine(orb.OnAutoClick, onClickDA);
		}, delegate
		{
			Orb orb = GameManager.Instance.Orb;
			orb.OnAutoClick = (Action<float>)Delegate.Remove(orb.OnAutoClick, onClickDA);
		}, null, 0f);
		list.Add(item5);
		effect_map.Add(Spells.DivineAlly, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Profit;
		simpleEffect.mult = 0.15000000596046448;
		simpleEffect.parameter = GameManager.Instance.BuildingManager.GetBuilding(2).TotalLevel;
		list.Add(simpleEffect);
		effect_map.Add(Spells.HallowedWritings, list);
		list = new List<IEffect>();
		RelentlessnessEffect item7 = new RelentlessnessEffect(2.0);
		list.Add(item7);
		effect_map.Add(Spells.Relentlessness, list);
		list = new List<IEffect>();
		HolyFervorEffect item8 = new HolyFervorEffect(2, 10, 10f, null);
		list.Add(item8);
		effect_map.Add(Spells.HolyFervor, list);
		NonscaledSpells.Add(Spells.HolyFervor);
		offline.Add(Spells.HolyFervor);
		list = new List<IEffect>();
		item8 = new HolyFervorEffect(2, 10, 10f, GetSpell(Spells.HolyFervor2).Use);
		list.Add(item8);
		effect_map.Add(Spells.HolyFervor2, list);
		NonscaledSpells.Add(Spells.HolyFervor2);
		offline.Add(Spells.HolyFervor2);
		list = new List<IEffect>();
		PowerOfSacrificeEffect powerOfSacrificeEffect = new PowerOfSacrificeEffect(1.2f);
		powerOfSacrificeEffect.pow_diminishing = 0.65f;
		list.Add(powerOfSacrificeEffect);
		effect_map.Add(Spells.PowerOfSacrifice, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentHero.AbilityPower;
		simpleEffect.mult = 0.004999999888241291;
		simpleEffect.parameter = GetSpell(Spells.ShatteringStrike).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ShatteringStrike, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(15f, 3f);
		list.Add(item2);
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Orb.critRating.crit_rating;
		simpleEffect.mult = 0.30000001192092896;
		simpleEffect.parameter = GameManager.Instance.CurrentHero.Level;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SpiritOfValor, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameContext.GetResource("Exorcist.MegaProfit"));
		simpleEffect.add = 0.0;
		simpleEffect.mult = 1.7999999523162842;
		simpleEffect.parameter = GameManager.Instance.CurrentHero.ClassBonusStacks;
		list.Add(simpleEffect);
		effect_map.Add(Spells.BattleTrance, list);
		list = new List<IEffect>();
		list.Add(new EffectAddProduction(100f, 10.0, 0.0, GameManager.Instance.CurrentHero.Hero.Level));
		EffectAddHeroBonus effectAddHeroBonus = new EffectAddHeroBonus(100f, 0.20000000298023224, 0.0, GameManager.Instance.CurrentHero.Hero.Level);
		effectAddHeroBonus.diminishing = 0f;
		list.Add(effectAddHeroBonus);
		effect_map.Add(Spells.SingularityBeam, list);
		list = new List<IEffect>();
		WarpEffect warpEffect = new WarpEffect(10);
		warpEffect.add = 0.5;
		warpEffect.mult = 0.0;
		warpEffect.parameter = GameManager.Instance.CurrentHero.Level;
		warpEffect.diminishing = 0.75f;
		list.Add(warpEffect);
		offline.Add(Spells.Wormhole);
		effect_map.Add(Spells.Wormhole, list);
		list = new List<IEffect>();
		TimeScaleEffect timeScaleEffect = new TimeScaleEffect(0.25f);
		timeScaleEffect.diminishing = 0.2f;
		list.Add(timeScaleEffect);
		effect_map.Add(Spells.TemporalDistortion, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(10f));
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.ShardsPool.Efficiency, 0.0, 5.0);
		list.Add(simpleEffect);
		effect_map.Add(Spells.GenerateParadox, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Profit;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 0.8999999761581421;
		simpleEffect.parameter = GetSpell(Spells.TimeHelix).Use;
		simpleEffect.diminishing = 0.95f;
		EffectTimeHelix effectTimeHelix = new EffectTimeHelix(simpleEffect);
		list.Add(effectTimeHelix);
		effect_map.Add(Spells.TimeHelix, list);
		on_choose.Add(Spells.TimeHelix, effectTimeHelix.OnSelect);
		on_clear.Add(Spells.TimeHelix, effectTimeHelix.OnDeselect);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentHero.AbilityPower;
		simpleEffect.mult = 0.15000000596046448;
		simpleEffect.parameter = GameManager.Instance.BuildingManager.GetBuilding(8).TotalLevel;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ConvergeTimelines, list);
		list = new List<IEffect>();
		item5 = new ActionEffect(delegate
		{
			List<Scroll> scrolls = GameManager.Instance.Scrolls.Scrolls;
			for (int i = 0; i < scrolls.Count; i++)
			{
				if (scrolls[i].spell != null)
				{
					if (scrolls[i].spell.active)
					{
						scrolls[i].TimeOfAction = 0f;
					}
					if (scrolls[i].spell.NameKey == Spells.Revert)
					{
						scrolls[i].spell.effects[0].Delete();
					}
				}
			}
		}, null, null, 0f);
		list.Add(item5);
		effect_map.Add(Spells.Revert, list);
		MemeticBanList.Add(Spells.Revert);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Profit);
		simpleEffect.add = 0.0;
		simpleEffect.mult = 2.5;
		EffectStabilizeTheFlow item9 = new EffectStabilizeTheFlow(simpleEffect);
		list.Add(item9);
		effect_map.Add(Spells.StabilizeTheFlow, list);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Profit);
		combineEffect.AddEffect(1.25, 0.5, GameManager.Instance.CurrentHero.SkipedPlayedTime, GameContext.GetEffect(EffectNames.Pow.ToString()));
		combineEffect.AddEffect(1.0, 0.8500000238418579, GetSpell(Spells.Superposition).Use, GameContext.GetEffect(EffectNames.Log10.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.Superposition, list);
		Action<Spells> add_charge_shadows = delegate(Spells x)
		{
			if (ShadowEnergyManager.instance != null && ShadowEnergyManager.instance.isActivated())
			{
				ShadowEnergyManager.instance.spells.Add(x);
			}
		};
		Action<Spells> remove_charge_shadows = delegate(Spells x)
		{
			if (ShadowEnergyManager.instance != null && ShadowEnergyManager.instance.isActivated())
			{
				ShadowEnergyManager.instance.spells.Remove(x);
			}
		};
		list = new List<IEffect>();
		list.Add(new FlashFire(20f, 0.009999999776482582, 0.0, GameManager.Instance.BuildingManager.GetBuilding(1).TotalLevel, 1f, 1.5f));
		effect_map.Add(Spells.FlashFire, list);
		on_choose.Add(Spells.FlashFire, delegate
		{
			add_charge_shadows(Spells.FlashFire);
		});
		on_clear.Add(Spells.FlashFire, delegate
		{
			remove_charge_shadows(Spells.FlashFire);
		});
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameContext.GetResource("Building.1.Profit");
		simpleEffect.add = 0.0002500000118743628;
		simpleEffect.mult = 1.0099999904632568;
		simpleEffect.parameter = GameManager.Instance.BuildingManager.GetBuilding(1).TotalLevel;
		list.Add(simpleEffect);
		effect_map.Add(Spells.CriticalMass, list);
		on_choose.Add(Spells.CriticalMass, delegate
		{
			add_charge_shadows(Spells.CriticalMass);
		});
		on_clear.Add(Spells.CriticalMass, delegate
		{
			remove_charge_shadows(Spells.CriticalMass);
		});
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		simpleEffect.target = GameContext.GetResource("Shadow.SpawnSpeed");
		simpleEffect.mult = 1.25;
		simpleEffect.diminishing = 0.8f;
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		simpleEffect.target = GameContext.GetResource("Shadow.LifeTime");
		simpleEffect.mult = 1.5;
		simpleEffect.diminishing = 0.1f;
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		simpleEffect.target = GameContext.GetResource("Shadow.BonusIncome");
		simpleEffect.diminishing = 0.5f;
		simpleEffect.mult = 2.0;
		list.Add(simpleEffect);
		effect_map.Add(Spells.CondensingShadows, list);
		on_choose.Add(Spells.CondensingShadows, delegate
		{
			add_charge_shadows(Spells.CondensingShadows);
		});
		on_clear.Add(Spells.CondensingShadows, delegate
		{
			remove_charge_shadows(Spells.CondensingShadows);
		});
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(4f));
		combineEffect = new CombineEffect(GameContext.GetResource("Shadow.IncomeMod"));
		combineEffect.AddEffect(0.001500000013038516, 0.6000000238418579, GameManager.Instance.BuildingManager.GetBuilding(1).TotalLevel, GameContext.GetEffect(EffectNames.PowA.ToString()), eff: true, 1f, 0.1f);
		combineEffect.AddEffect(1.0, 0.5, GetSpell(Spells.OnyxHound).Use, GameContext.GetEffect(EffectNames.PowA.ToString()), eff: false);
		list.Add(combineEffect);
		list.Add(new DrainShadows(Spells.OnyxHound, 250.0, 0.0));
		list.Add(new EffectAutoVoidCollect(45f, Statistic.PetMaxLevel, null));
		effect_map.Add(Spells.OnyxHound, list);
		list = new List<IEffect>();
		list.Add(new EffectAddOfflineProduction(10f, 0.10000000149011612, 0.0, GameManager.Instance.CurrentHero.Level, 0.1f, 0.53f));
		list.Add(new EffectAddShadows(10f, 0.0, 0.009999999776482582, GameManager.Instance.CurrentHero.Level, 0.5f));
		effect_map.Add(Spells.DayIntoNight, list);
		list = new List<IEffect>();
		list.Add(new EffectDancingFlame(5f, 1f, 1f, 1f, crit_const: false, 1f, null, 15f));
		effect_map.Add(Spells.DancingFlame, list);
		list = new List<IEffect>();
		list.Add(new EffectSeetheInShadows(0.10000000149011612, 0.6000000238418579, 1.0, 0.75));
		effect_map.Add(Spells.SeetheInShadows, list);
		on_choose.Add(Spells.SeetheInShadows, delegate
		{
			add_charge_shadows(Spells.SeetheInShadows);
		});
		on_clear.Add(Spells.SeetheInShadows, delegate
		{
			remove_charge_shadows(Spells.SeetheInShadows);
		});
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameManager.Instance.OfflineProduction;
		simpleEffect.add = 2.5;
		simpleEffect.mult = 0.8999999761581421;
		simpleEffect.parameter = GetSpell(Spells.Eclipse).Use;
		list.Add(simpleEffect);
		list.Add(new DrainShadows(Spells.Eclipse, 500.0, 0.0));
		effect_map.Add(Spells.Eclipse, list);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Profit);
		combineEffect.AddEffect(0.0, 0.0020000000949949026, GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, GameContext.GetEffect(EffectNames.Linear.ToString()), eff: true, 0.5f);
		combineEffect.AddEffect(0.0, 1.0, GameManager.Instance.VoidManaManager.Power, GameContext.GetEffect(EffectNames.Linear.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.ShadowsOfTheVoid, list);
		on_choose.Add(Spells.ShadowsOfTheVoid, delegate
		{
			add_charge_shadows(Spells.ShadowsOfTheVoid);
		});
		on_clear.Add(Spells.ShadowsOfTheVoid, delegate
		{
			remove_charge_shadows(Spells.ShadowsOfTheVoid);
		});
		list = new List<IEffect>();
		list.Add(new EffectUmbralRage(GameContext.GetEffect("PowA"), GameManager.Instance.Scrolls.EvocationEfficiency, 19.0, 0.5));
		effect_map.Add(Spells.UmbralRage, list);
		on_choose.Add(Spells.UmbralRage, delegate
		{
			add_charge_shadows(Spells.UmbralRage);
		});
		on_clear.Add(Spells.UmbralRage, delegate
		{
			remove_charge_shadows(Spells.UmbralRage);
		});
		list = new List<IEffect>();
		List<int> list4 = new List<int>(1);
		list4.Add(1);
		list.Add(new EffectTransmutation(100, list4, 0.01f));
		effect_map.Add(Spells.Transmute, list);
		MemeticBanList.Add(Spells.Transmute);
		list = new List<IEffect>();
		Action<int> GiveMG = delegate(int y)
		{
			if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Alchemist && y >= 0)
			{
				Building building = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 1).building;
				int num = GameManager.Instance.CurrentHero.ClassBonusStacks.Value.ToInt() - building.Level.ValueInt;
				if (num > 0)
				{
					num /= 5;
					if (num == 0)
					{
						num = 1;
					}
					building.Level.Change(num);
					Statistic.TotalBuildings.Change(num);
				}
			}
		};
		Action apply = delegate
		{
			for (int i = 0; i < GameManager.Instance.Buildings.Count; i++)
			{
				if (GameManager.Instance.Buildings[i].building.Tier != 1)
				{
					VariableInt level = GameManager.Instance.Buildings[i].building.Level;
					level.OnChangeInt = (Action<int>)Delegate.Combine(level.OnChangeInt, GiveMG);
				}
			}
		};
		Action delete = delegate
		{
			for (int i = 0; i < GameManager.Instance.Buildings.Count; i++)
			{
				if (GameManager.Instance.Buildings[i].building.Tier != 1)
				{
					VariableInt level = GameManager.Instance.Buildings[i].building.Level;
					level.OnChangeInt = (Action<int>)Delegate.Remove(level.OnChangeInt, GiveMG);
				}
			}
		};
		item5 = new ActionEffect(apply, delete, null, 0f);
		list.Add(item5);
		effect_map.Add(Spells.DraughtOfMidas, list);
		MemeticBanList.Add(Spells.DraughtOfMidas);
		list = new List<IEffect>();
		list.Add(new EffectAddVoidManaW(1000f, 50.0, 5f, GetSpell(Spells.VoidDecompression).Use, 0f, 0.5f));
		effect_map.Add(Spells.VoidDecompression, list);
		list = new List<IEffect>();
		effectAddShardInstant = new EffectAddShardInstant(100.0, 0.0, 0.009999999776482582, GetSpell(Spells.Crystallization).Use);
		effectAddShardInstant.pow_diminishing = 0.1f;
		list.Add(effectAddShardInstant);
		effect_map.Add(Spells.Crystallization, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameManager.Instance.CurrentHero.AbilityPower;
		simpleEffect.add = 0.0010000000474974513;
		simpleEffect.mult = 0.44999998807907104;
		simpleEffect.parameter = GetSpell(Spells.CondensedEnergy).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.CondensedEnergy, list);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Orb.autoclick_profit, 0.009999999776482582, 0.6000000238418579, GetSpell(Spells.Denaturation).Use, EffectNames.PowA)
		};
		effect_map.Add(Spells.Denaturation, list);
		list = new List<IEffect>
		{
			new EffectAutoClick(10f),
			new SimpleEffect(Reborn.SoulPower, 0.10000000149011612, 0.4000000059604645, Statistic.ResourcesCollected, EffectNames.Pow, 0.4f),
			new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 0.012500000186264515, 0.22499999403953552, Statistic.CastSpell, EffectNames.PowA, 0.65f)
		};
		effect_map.Add(Spells.ArtificialMuse, list);
		list = new List<IEffect>
		{
			new EffectAutoClick(10f),
			new SimpleEffect(GameManager.Instance.Profit, 0.0, 0.012500000186264515, GameManager.Instance.BuildingManager.GetBuilding(5).TotalLevel, EffectNames.Linear, 0.7f),
			new SimpleEffect(GameManager.Instance.VoidManaManager.Power, 0.0, 1.25, EffectNames.Linear, 0.75f)
		};
		effect_map.Add(Spells.Automatise, list);
		list = new List<IEffect>
		{
			new EffectAddProduction(10f)
		};
		effect_map.Add(Spells.AnimaSynteta, list);
		list = new List<IEffect>();
		list.Add(new MultiChargesEffect(100f, Spells.FuriousStrike, 0.1f));
		effect_map.Add(Spells.FuriousStrike, list);
		list = new List<IEffect>();
		list.Add(new EffectTemperTheSteelPeriodic(1f, 0.05f, 0.001f, 0f, GetSpell(Spells.TemperTheSteel).UseThisRun, 0.9f, Spells.TemperTheSteel));
		list.Add(new EffectTemperTheSteelPersistent(Spells.TemperTheSteel));
		effect_map.Add(Spells.TemperTheSteel, list);
		MemeticBanList.Add(Spells.TemperTheSteel);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 0.004999999888241291;
		simpleEffect.mult = 1.1799999475479126;
		simpleEffect.parameter = GetSpell(Spells.IronBlood).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.IronBlood, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Profit);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 0.004999999888241291;
		simpleEffect.mult = 1.2000000476837158;
		simpleEffect.parameter = GetSpell(Spells.EnhancedStrength).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.EnhancedStrength, list);
		list = new List<IEffect>();
		EffectAddShardPeriodic effectAddShardPeriodic = new EffectAddShardPeriodic(1f, 1000.0, 1.0, 0.0, GetSpell(Spells.ForceOfWill).Use);
		effectAddShardPeriodic.pow_diminishing = 0.1f;
		list.Add(effectAddShardPeriodic);
		effect_map.Add(Spells.ForceOfWill, list);
		list = new List<IEffect>();
		list.Add(new EffectAddProduction(5f, 0.0, 1.0, GameManager.Instance.CurrentHero.Level));
		effect_map.Add(Spells.CounterSpell, list);
		list = new List<IEffect>();
		list.Add(new EffectAddProduction(10f, 0.10000000149011612, 1.25, GameManager.Instance.CurrentHero.ClassBonusStacks));
		effect_map.Add(Spells.Debilitate, list);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Scrolls.EvocationEfficiency);
		combineEffect.AddEffect(5.0, 0.8999999761581421, GetSpell(Spells.NullZone).Use, GameContext.GetEffect(EffectNames.Pow.ToString()));
		combineEffect.AddEffect(0.5, 0.949999988079071, GameManager.Instance.BuildingManager.GetBuilding(6).TotalLevel, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.NullZone, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Profit);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 0.0010000000474974513;
		simpleEffect.mult = 1.399999976158142;
		simpleEffect.parameter = GameManager.Instance.Scrolls.ShardsPassive;
		list.Add(simpleEffect);
		effect_map.Add(Spells.FireWithFire, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.ShardsPassive);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 0.5;
		simpleEffect.mult = 0.8500000238418579;
		simpleEffect.parameter = GetSpell(Spells.SyphonPower).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SyphonPower, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.SpellShardsCostReduction);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 1.0;
		simpleEffect.mult = 1.5;
		simpleEffect.diminishing = 0f;
		simpleEffect.parameter = GetSpell(Spells.Silence).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.Silence, list);
		NonscaledSpells.Add(Spells.Silence);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(4f));
		value = delegate
		{
			(GameManager.Instance.CurrentPet.PetPanel.PetMap[PetNames.Doppelganger] as Doppelganger).isLoseExp = false;
		};
		on_choose.Add(Spells.MaterialiseDoppelganger, value);
		value2 = delegate
		{
			(GameManager.Instance.CurrentPet.PetPanel.PetMap[PetNames.Doppelganger] as Doppelganger).isLoseExp = true;
		};
		on_clear.Add(Spells.MaterialiseDoppelganger, value2);
		item5 = new ActionEffect(null, delegate
		{
			if (GameManager.Instance.CurrentPet.Pet != null && GameManager.Instance.CurrentPet.Pet.NameKey == PetNames.Doppelganger)
			{
				GameManager.Instance.CurrentPet.Pet.ResetExp();
			}
		}, delegate(BigNumber eff)
		{
			if (GameManager.Instance.CurrentPet.Pet != null)
			{
				float num = GameManager.Instance.CurrentHero.Level.ValueInt - 100;
				if (num <= 0f)
				{
					num = 1f;
				}
				num /= 100f;
				num += 1f;
				num *= num;
				BigNumber exp = GameManager.Instance.CurrentHero.HeroExp.Value.Pow(0.05f + 0.1f * num);
				exp *= 1.0 + eff.Pow(1.5);
				exp *= 1.0 + (GetSpell(Spells.MaterialiseDoppelganger).Use.Value / 10.0).Pow(0.5);
				GameManager.Instance.CurrentPet.Pet.AddExp(exp);
			}
		}, 0.25f);
		list.Add(item5);
		effect_map.Add(Spells.MaterialiseDoppelganger, list);
		MemeticBanList.Add(Spells.MaterialiseDoppelganger);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(6f));
		simpleEffect = new SimpleEffect(GameManager.Instance.Orb.critRating.crit_rating);
		simpleEffect.effect = GameContext.GetEffect(EffectNames.Pow.ToString());
		simpleEffect.add = 1.0;
		simpleEffect.mult = 0.75;
		simpleEffect.pow_diminishing = 0.7f;
		simpleEffect.parameter = Statistic.AutoClicks;
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.Profit);
		simpleEffect.mult = 1.5;
		simpleEffect.pow_diminishing = 0.7f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SummonSpiderSwarm, list);
		list = new List<IEffect>();
		list.Add(new EffectOrnaments());
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Orb.click_profit;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 2.5;
		simpleEffect.pow_diminishing = 0.5f;
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameManager.Instance.VoidManaManager.Power;
		simpleEffect.add = 0.023000000044703484;
		simpleEffect.mult = 0.5;
		simpleEffect.pow_diminishing = 0.5f;
		simpleEffect.parameter = Statistic.HCTotal;
		list.Add(simpleEffect);
		effect_map.Add(Spells.VoidforgedArmaments, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Orb.autoclick_profit);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 5E-06;
		simpleEffect.mult = 1.25;
		simpleEffect.parameter = Statistic.AutoClicks;
		simpleEffect.pow_diminishing = 0.65f;
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.Orb.click_profit);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 1.25E-05;
		simpleEffect.mult = 1.149999976158142;
		simpleEffect.parameter = Statistic.CastSpell;
		simpleEffect.pow_diminishing = 0.65f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ForsakenGlory, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency);
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.add = 1.0;
		simpleEffect.mult = 0.8;
		simpleEffect.pow_diminishing = 1.3f;
		simpleEffect.parameter = Statistic.PetMaxLevel;
		list.Add(simpleEffect);
		effect_map.Add(Spells.AbyssalStrike, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentPet.AbilityPower, 0.8999999761581421, 0.30000001192092896, GameManager.Instance.BuildingManager.GetBuilding(6).TotalLevel);
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.pow_diminishing = 0.75f;
		list.Add(simpleEffect);
		list.Add(new EffectAddProductionPeriodicHellrage(4f, 0.1f));
		effect_map.Add(Spells.MeditativeFury, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower);
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.add = 9.999999747378752E-05;
		simpleEffect.mult = 1.4500000476837158;
		simpleEffect.parameter = GetSpell(Spells.NotAI).Use;
		simpleEffect.pow_diminishing = 0.25f;
		Action onUseNotAI = delegate
		{
			GetSpell(Spells.NotAI).Delete();
			GetSpell(Spells.NotAI).Apply();
		};
		value = delegate
		{
			VariableComplex use = GetSpell(Spells.NotAI).Use;
			use.OnChange = (Action)Delegate.Combine(use.OnChange, onUseNotAI);
		};
		on_choose.Add(Spells.NotAI, value);
		value2 = delegate
		{
			VariableComplex use = GetSpell(Spells.NotAI).Use;
			use.OnChange = (Action)Delegate.Remove(use.OnChange, onUseNotAI);
		};
		on_clear.Add(Spells.NotAI, value2);
		list.Add(simpleEffect);
		TAEffect tAEffect = new TAEffect();
		tAEffect.target = GameManager.Instance.Scrolls.SpellShardsCostReduction;
		tAEffect.add = 0.05000000074505806;
		tAEffect.mult = 0.0;
		tAEffect.diminishing = 0f;
		tAEffect.parameter = GetSpell(Spells.NotAI).Use;
		list.Add(tAEffect);
		effect_map.Add(Spells.NotAI, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(20f));
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentPet.ExpBonus, 9.999999747378752E-06, 0.4000000059604645, GetSpell(Spells.ExpelDoppelganger).Use, EffectNames.Pow);
		simpleEffect.diminishing = 0.5f;
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentPet.AbilityPower, 9.999999747378752E-06, 0.5, GetSpell(Spells.ExpelDoppelganger).Use, EffectNames.Pow);
		simpleEffect.diminishing = 0.5f;
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffectInt(GameManager.Instance.Scrolls.MaxCharge, 1.0, 1.0);
		simpleEffect.diminishing = 0.4f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ExpelDoppelganger, list);
		list = new List<IEffect>();
		warpEffect = new WarpEffect(20, stopSpells: false);
		warpEffect.add = 0.5;
		warpEffect.mult = 0.0;
		warpEffect.parameter = GameManager.Instance.CurrentHero.Level;
		warpEffect.diminishing = 0.5f;
		list.Add(warpEffect);
		effect_map.Add(Spells.CraftedWormhole, list);
		Action<float> crafted_wormhole_tick = delegate(float k)
		{
			Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == Spells.CraftedWormhole);
			if (!(scroll == null))
			{
				k = Time.deltaTime * (1f + Time.timeScale / 4f);
				scroll.AddProgressBuild(k, text: false, shards: false);
			}
		};
		value = delegate
		{
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Combine(instance.GameTick, crafted_wormhole_tick);
		};
		on_choose.Add(Spells.CraftedWormhole, value);
		value2 = delegate
		{
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Remove(instance.GameTick, crafted_wormhole_tick);
		};
		on_clear.Add(Spells.CraftedWormhole, value2);
		offline.Add(Spells.CraftedWormhole);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Profit);
		combineEffect.AddEffect(0.20000000298023224, 0.7099999785423279, GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, GameContext.GetEffect(EffectNames.PowA.ToString()), eff: true, 0.5f);
		combineEffect.AddEffect(1.0, 0.7099999785423279, GameManager.Instance.VoidManaManager.Power, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		combineEffect.AddEffect(0.009999999776482582, 1.0499999523162842, GetSpell(Spells.BurningVoid).Use, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		combineEffect = new CombineEffect(GameManager.Instance.Orb.click_profit);
		combineEffect.AddEffect(1.0, 0.6200000047683716, GameContext.GetResource("Shadow.ShadowEnergy"), GameContext.GetEffect(EffectNames.Pow.ToString()), eff: true, 0.5f);
		combineEffect.AddEffect(1.0, 1.100000023841858, GetSpell(Spells.BurningVoid).Use, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.BurningVoid, list);
		on_choose.Add(Spells.BurningVoid, delegate
		{
			add_charge_shadows(Spells.BurningVoid);
		});
		on_clear.Add(Spells.BurningVoid, delegate
		{
			remove_charge_shadows(Spells.BurningVoid);
		});
		list = new List<IEffect>();
		Action<int> GiveMG2 = delegate(int y)
		{
			if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Desolator && y >= 0)
			{
				Building building = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 1).building;
				int amountAvailable = building.GetAmountAvailable(Statistic.ManaSession.Value);
				if (amountAvailable > 0)
				{
					amountAvailable /= 5;
					if (amountAvailable == 0)
					{
						amountAvailable = 1;
					}
					building.Level.Change(amountAvailable);
					Statistic.TotalBuildings.Change(amountAvailable);
				}
			}
		};
		Action apply2 = delegate
		{
			for (int i = 0; i < GameManager.Instance.Buildings.Count; i++)
			{
				if (GameManager.Instance.Buildings[i].building.Tier != 1)
				{
					VariableInt level = GameManager.Instance.Buildings[i].building.Level;
					level.OnChangeInt = (Action<int>)Delegate.Combine(level.OnChangeInt, GiveMG2);
				}
			}
		};
		Action delete2 = delegate
		{
			for (int i = 0; i < GameManager.Instance.Buildings.Count; i++)
			{
				if (GameManager.Instance.Buildings[i].building.Tier != 1)
				{
					VariableInt level = GameManager.Instance.Buildings[i].building.Level;
					level.OnChangeInt = (Action<int>)Delegate.Remove(level.OnChangeInt, GiveMG2);
				}
			}
		};
		item5 = new ActionEffect(apply2, delete2, null, 0f);
		list.Add(item5);
		effect_map.Add(Spells.DraughtOfMidas2, list);
		MemeticBanList.Add(Spells.DraughtOfMidas2);
		list = new List<IEffect>();
		list.Add(new BurnShadows(0.0001f));
		effect_map.Add(Spells.BurnAllThatBurns, list);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Scrolls.EvocationEfficiency);
		combineEffect.AddEffect(5.0, 0.8999999761581421, GetSpell(Spells.Permafrost).Use, GameContext.GetEffect(EffectNames.Pow.ToString()));
		combineEffect.AddEffect(0.004999999888241291, 0.949999988079071, GameManager.Instance.BuildingManager.GetBuilding(3).TotalLevel, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.Permafrost, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Profit, 1.0, 1.0, GameManager.Instance.CurrentHero.ClassBonusStacks, EffectNames.Pow);
		list.Add(simpleEffect);
		effect_map.Add(Spells.Blizzard, list);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Scrolls.SpellShardsCostReduction);
		combineEffect.AddEffect(0.0, 1.5, GetSpell(Spells.StillArcana).Use, GameContext.GetEffect(EffectNames.Linear.ToString()), eff: false);
		combineEffect.AddEffect(1.0, 0.5, GameManager.Instance.CurrentHero.ClassBonusStacks, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.StillArcana, list);
		NonscaledSpells.Add(Spells.StillArcana);
		list = new List<IEffect>();
		effectAddShardInstant = new EffectAddShardInstant(100.0, 0.0, 0.009999999776482582, GetSpell(Spells.HeatExchange).Use);
		effectAddShardInstant.pow_diminishing = 0.1f;
		list.Add(effectAddShardInstant);
		list.Add(new EffectFunction(delegate
		{
			if (GameManager.Instance.CurrentHero.Hero is Frostmage)
			{
				(GameManager.Instance.CurrentHero.Hero as Frostmage).AddTempReduction(0.001f);
			}
		}));
		effect_map.Add(Spells.HeatExchange, list);
		list = new List<IEffect>();
		EffectAddProduction effectAddProduction = new EffectAddProduction(1f, 0.0, 0.0);
		effectAddProduction.pow_diminishing = 1.2f;
		list.Add(effectAddProduction);
		effect_map.Add(Spells.FlashFreeze, list);
		list = new List<IEffect>();
		list.Add(new EffectAddProduction(1f, 0.0, 0.0));
		list.Add(new EffectFunction(delegate
		{
			foreach (Scroll scroll2 in GameManager.Instance.Scrolls.Scrolls)
			{
				if (scroll2.spell != null && scroll2.active)
				{
					scroll2.TimeOfAction += 1f;
				}
			}
		}));
		effect_map.Add(Spells.ShatterMagic, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.Scrolls.ShardsPool.Capacity, 0.0, 12.0));
		effect_map.Add(Spells.InfusedGlacialFormations, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(10f));
		list.Add(new SimpleEffectSnap(GameManager.Instance.VoidManaManager.Power, 0.001, 1.0, GetSpell(Spells.ConjureFreezingMist).Use, EffectNames.Pow));
		list.Add(new EffectFunction(GameManager.Instance.Idle.ActivateIdle));
		effect_map.Add(Spells.ConjureFreezingMist, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(4f));
		list.Add(new SimpleEffect(GameContext.GetResource("Shadow.IncomeMod"), 0.001, 1.05, GetSpell(Spells.SumLSGain).Use, EffectNames.Pow));
		simpleEffect = new SimpleEffect(GameContext.GetResource("Shadow.SpawnSpeed"), 0.0, 1.5);
		simpleEffect.diminishing = 0.9f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.SumLSGain, list);
		list = new List<IEffect>();
		list.Add(new MultiChargesEffect(0f, Spells.ShadowAug, 0.1f, 0f));
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower, 0.1, 1.01, GetSpell(Spells.ShadowAug).Use, EffectNames.Pow);
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ShadowAug, list);
		on_choose.Add(Spells.ShadowAug, delegate
		{
			add_charge_shadows(Spells.ShadowAug);
		});
		on_clear.Add(Spells.ShadowAug, delegate
		{
			remove_charge_shadows(Spells.ShadowAug);
		});
		NonscaledSpells.Add(Spells.ShadowAug);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameContext.GetResource("Nosferatu.BloodIncome"), 0.0001, 0.75, GameContext.GetResource("Shadow.ShadowEnergy"), EffectNames.Pow, 0.25f));
		effect_map.Add(Spells.BloodIncome, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(20f));
		list.Add(new EffectAddBloodPerClick(1f, 20f));
		effect_map.Add(Spells.SumBlood1, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(1f));
		list.Add(new EffectAddBloodPerClick(50f, 1f));
		effect_map.Add(Spells.SumBlood2, list);
		list = new List<IEffect>();
		list.Add(new BloodToPetXP(0.1f, 1E-06f, 0.1f));
		effect_map.Add(Spells.BloodToPetXP, list);
		list = new List<IEffect>();
		effectAddProduction = new EffectAddProduction(120f, 0.0, 0.0);
		effectAddProduction.pow_diminishing = 1.05f;
		list.Add(effectAddProduction);
		effect_map.Add(Spells.BloodMana, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.Profit, 0.0, 1.0, GameManager.Instance.CurrentHero.ClassBonusStacks));
		effect_map.Add(Spells.BloodProduction, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameContext.GetResource("Building.4.Profit"), 9.999999747378752E-06, 2.0, GameManager.Instance.BuildingManager.GetBuilding(4).TotalLevel, EffectNames.PowA, 1.3f));
		effect_map.Add(Spells.HuntingGrounds, list);
		on_choose.Add(Spells.HuntingGrounds, delegate
		{
			add_charge_shadows(Spells.HuntingGrounds);
		});
		on_clear.Add(Spells.HuntingGrounds, delegate
		{
			remove_charge_shadows(Spells.HuntingGrounds);
		});
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.Profit, 0.0, 0.0010000000474974513, GameManager.Instance.OfflineProduction));
		effect_map.Add(Spells.OfflineToProduction, list);
		on_choose.Add(Spells.OfflineToProduction, delegate
		{
			add_charge_shadows(Spells.OfflineToProduction);
		});
		on_clear.Add(Spells.OfflineToProduction, delegate
		{
			remove_charge_shadows(Spells.OfflineToProduction);
		});
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.CurrentPet.AbilityPower, 0.01, 1.100000023841858, Statistic.BatsExile, EffectNames.PowA));
		effect_map.Add(Spells.HellrageBats, list);
		Action<Spells> choose_hull = delegate(Spells x)
		{
			foreach (Scroll scroll3 in GameManager.Instance.Scrolls.Scrolls)
			{
				if (scroll3.spell != null && x != scroll3.spell.NameKey && (scroll3.spell.NameKey == Spells.HullDurReduction || scroll3.spell.NameKey == Spells.HullVoidProfits || scroll3.spell.NameKey == Spells.HullVpE))
				{
					scroll3.Clear();
				}
			}
		};
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 0.0, 1.25, GameContext.GetResource("Artificer.Parts"), EffectNames.Linear, 0.5f)
		};
		effect_map.Add(Spells.HullVpE, list);
		on_choose.Add(Spells.HullVpE, delegate
		{
			choose_hull(Spells.HullVpE);
		});
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.VoidManaManager.Power, 0.0, 1.25, GameContext.GetResource("Artificer.Parts"), EffectNames.Linear, 0.5f)
		};
		effect_map.Add(Spells.HullVoidProfits, list);
		on_choose.Add(Spells.HullVoidProfits, delegate
		{
			choose_hull(Spells.HullVoidProfits);
		});
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Scrolls.IncantationDurationReduction, 0.0, 1.0499999523162842, EffectNames.Linear, 0.1f)
		};
		effect_map.Add(Spells.HullDurReduction, list);
		on_choose.Add(Spells.HullDurReduction, delegate
		{
			choose_hull(Spells.HullDurReduction);
		});
		Action<Spells> choose_processor = delegate(Spells x)
		{
			foreach (Scroll scroll4 in GameManager.Instance.Scrolls.Scrolls)
			{
				if (scroll4.spell != null && x != scroll4.spell.NameKey && (scroll4.spell.NameKey == Spells.ProcessorMystPower || scroll4.spell.NameKey == Spells.ProcessorPAP))
				{
					scroll4.Clear();
				}
			}
		};
		list = new List<IEffect>
		{
			new SimpleEffect(Reborn.SoulPower, 1.0, 1.0499999523162842, GetSpell(Spells.ProcessorMystPower).Use, EffectNames.Pow, 1.2f)
		};
		effect_map.Add(Spells.ProcessorMystPower, list);
		on_choose.Add(Spells.ProcessorMystPower, delegate
		{
			choose_processor(Spells.ProcessorMystPower);
		});
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.CurrentPet.AbilityPower, 1.0, 1.0499999523162842, GetSpell(Spells.ProcessorPAP).Use, EffectNames.Pow)
		};
		effect_map.Add(Spells.ProcessorPAP, list);
		on_choose.Add(Spells.ProcessorPAP, delegate
		{
			choose_processor(Spells.ProcessorPAP);
		});
		Action<Spells> choose_function = delegate(Spells x)
		{
			foreach (Scroll scroll5 in GameManager.Instance.Scrolls.Scrolls)
			{
				if (scroll5.spell != null && x != scroll5.spell.NameKey && (scroll5.spell.NameKey == Spells.FuncBuild || scroll5.spell.NameKey == Spells.FuncMana || scroll5.spell.NameKey == Spells.FuncVmana))
				{
					scroll5.Clear();
				}
			}
		};
		list = new List<IEffect>
		{
			new ArtificerFunctionMana(GameManager.Instance.ManaManager.Mana, 10f, GameManager.Instance.PPS, 1f, 1.2f, 1f, 1f)
		};
		effect_map.Add(Spells.FuncMana, list);
		on_choose.Add(Spells.FuncMana, delegate
		{
			choose_function(Spells.FuncMana);
		});
		list = new List<IEffect>
		{
			new ArtificerFunctionVMana(GameManager.Instance.VoidMana, 1f, GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 1f, 1f, 0.2f, 0.8f)
		};
		effect_map.Add(Spells.FuncVmana, list);
		on_choose.Add(Spells.FuncVmana, delegate
		{
			choose_function(Spells.FuncVmana);
		});
		list = new List<IEffect>
		{
			new ArtificerFunction(GameContext.GetResource("Artificer.Parts"), 1f, null, 0.0001f, 0.2f, 0f, 1f)
		};
		effect_map.Add(Spells.FuncBuild, list);
		on_choose.Add(Spells.FuncBuild, delegate
		{
			choose_function(Spells.FuncBuild);
		});
		NonscaledSpells.Add(Spells.FuncBuild);
		MemeticBanList.Add(Spells.FuncBuild);
		Action<Spells> choose_engine = delegate(Spells x)
		{
			foreach (Scroll scroll6 in GameManager.Instance.Scrolls.Scrolls)
			{
				if (scroll6.spell != null && x != scroll6.spell.NameKey && (scroll6.spell.NameKey == Spells.EngineEvo || scroll6.spell.NameKey == Spells.EngineProfit))
				{
					scroll6.Clear();
				}
			}
		};
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 1.0, 1.0, GameContext.GetResource("Artificer.Energy"), EffectNames.Pow)
		};
		effect_map.Add(Spells.EngineEvo, list);
		on_choose.Add(Spells.EngineEvo, delegate
		{
			choose_engine(Spells.EngineEvo);
		});
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Profit, 1.0, 1.0, GameContext.GetResource("Artificer.Energy"), EffectNames.Pow)
		};
		effect_map.Add(Spells.EngineProfit, list);
		on_choose.Add(Spells.EngineProfit, delegate
		{
			choose_engine(Spells.EngineProfit);
		});
		Action<Spells> choose_generator = delegate(Spells x)
		{
			foreach (Scroll scroll7 in GameManager.Instance.Scrolls.Scrolls)
			{
				if (scroll7.spell != null && x != scroll7.spell.NameKey && (scroll7.spell.NameKey == Spells.GeneratorVpe || scroll7.spell.NameKey == Spells.GeneratorInca))
				{
					scroll7.Clear();
				}
			}
		};
		list = new List<IEffect>
		{
			new ArtificerGenerator(10f, GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 0.3f)
		};
		effect_map.Add(Spells.GeneratorVpe, list);
		on_choose.Add(Spells.GeneratorVpe, delegate
		{
			choose_generator(Spells.GeneratorVpe);
		});
		NonscaledSpells.Add(Spells.GeneratorVpe);
		MemeticBanList.Add(Spells.GeneratorVpe);
		list = new List<IEffect>
		{
			new ArtificerGenerator(10f, GameManager.Instance.Scrolls.IncantationEfficiency, 1.2f)
		};
		effect_map.Add(Spells.GeneratorInca, list);
		on_choose.Add(Spells.GeneratorInca, delegate
		{
			choose_generator(Spells.GeneratorInca);
		});
		NonscaledSpells.Add(Spells.GeneratorInca);
		MemeticBanList.Add(Spells.GeneratorInca);
		Action func = delegate
		{
			GameContext.GetResource("Artificer.Parts").Change(1.0, 1.0);
		};
		list = new List<IEffect>
		{
			new EffectFunction(func)
		};
		effect_map.Add(Spells.HandBuild, list);
		NonscaledSpells.Add(Spells.HandBuild);
		MemeticBanList.Add(Spells.HandBuild);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Profit, 0.0010000000474974513, 1.0499999523162842, EffectNames.Pow);
		simpleEffect.parameter = GetSpell(Spells.AugmentProfit).Use;
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.AugmentProfit, list);
		NonscaledSpells.Add(Spells.AugmentProfit);
		MemeticBanList.Add(Spells.AugmentProfit);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 9.999999747378752E-05, 1.0499999523162842, EffectNames.Pow);
		simpleEffect.parameter = GetSpell(Spells.ShapeEvoAug).Use;
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ShapeEvoAug, list);
		NonscaledSpells.Add(Spells.ShapeEvoAug);
		MemeticBanList.Add(Spells.ShapeEvoAug);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower, 0.0010000000474974513, 1.0499999523162842, EffectNames.Pow);
		simpleEffect.parameter = GetSpell(Spells.ShapeCapAug).Use;
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ShapeCapAug, list);
		NonscaledSpells.Add(Spells.ShapeCapAug);
		MemeticBanList.Add(Spells.ShapeCapAug);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.AccumulatedCasts, 0.0010000000474974513, 0.75, EffectNames.Pow);
		simpleEffect.parameter = GetSpell(Spells.ShapeAcumAug).Use;
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ShapeAcumAug, list);
		NonscaledSpells.Add(Spells.ShapeAcumAug);
		MemeticBanList.Add(Spells.ShapeAcumAug);
		list = new List<IEffect>();
		list.Add(new EffectAddProduction(1f, 0.009999999776482582, 0.0, GetSpell(Spells.ShapeBurst).Use));
		effect_map.Add(Spells.ShapeBurst, list);
		list = new List<IEffect>();
		effectUseMM = new EffectUseMM(Spells.ShapeBurst, 0.1f);
		effectUseMM.base_count = 10;
		list.Add(effectUseMM);
		effect_map.Add(Spells.ShapeMassBurst, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.Profit, 1E-05, 1.0, GameManager.Instance.Idle.IdleBonus, EffectNames.PowA));
		effect_map.Add(Spells.ShapeIdleProfit, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.Profit, 0.001, 1.2, Reborn.SoulPowerTotal, EffectNames.PowA));
		effect_map.Add(Spells.ShapeMyst, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.Profit, 0.001, 0.53, GameManager.Instance.CurrentHero.ClassBonusStacks, EffectNames.PowA));
		effect_map.Add(Spells.ShapeSatietyProfit, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.Profit, 0.001, 1.1, GameContext.GetResource("Building.6.TotalLevel"), EffectNames.PowA));
		effect_map.Add(Spells.ShapeSource, list);
		list = new List<IEffect>();
		list.Add(new EffectGainSatiety());
		effect_map.Add(Spells.ShapeSatietyGain, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameContext.GetResource("Shapeshifter.Income"), 0.1, 0.5, GameManager.Instance.Idle.IdleBonus, EffectNames.PowA));
		effect_map.Add(Spells.ShapeSatietyIncomeIdle, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameContext.GetResource("Shapeshifter.Income"), 0.01, 0.5, Statistic.VoidManaSession, EffectNames.PowA));
		effect_map.Add(Spells.ShapeSatietyIncomeVoid, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoclickVariable(1f, 1f, GameManager.Instance.CurrentHero.ClassBonusStacks, new EffectAutoClick(1f, 1f, 1f, 1f, crit_const: false, 1f, fromSpell: false), useLog: true, fromSpell: true));
		effect_map.Add(Spells.ShapeClicks, list);
		list = new List<IEffect>
		{
			new ActionEffect(delegate
			{
				List<Scroll> list5 = GameManager.Instance.Scrolls.Scrolls.FindAll((Scroll x) => x.spell != null && !x.spell.ShardsBuilding);
				if (list5 == null)
				{
					return;
				}
				foreach (Scroll item10 in list5)
				{
					item10.spell.AddProgressBuild(0.10000000149011612 * item10.spell.FullBuild, shards: false);
				}
			}, null, null, 0f, isInstant: true)
		};
		effect_map.Add(Spells.ShapeAugCharge, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 0.0, 1.5, EffectNames.Linear, 0.75f));
		effect_map.Add(Spells.ShapeVPE, list);
		list = new List<IEffect>();
		list.Add(new EffectAddCollectItem(1, 0f, 1.2f));
		effect_map.Add(Spells.ShapeVE, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 1.0, 0.800000011920929, GameManager.Instance.Idle.IdleBonus, EffectNames.Pow));
		effect_map.Add(Spells.ShapeIdleVPE, list);
		list = new List<IEffect>();
		list.Add(new EffectBuyManaSource(1f, 10f, GameManager.Instance.CurrentHero.ClassBonusStacks, 0.1f));
		effect_map.Add(Spells.ShapeBuySources, list);
		list = new List<IEffect>
		{
			new EffectArcherTention()
		};
		effect_map.Add(Spells.DrawTheBowstring, list);
		value = delegate
		{
			foreach (Spell availableSpell in AvailableSpells)
			{
				if (availableSpell.Type == SpellTypeGroup.None)
				{
					availableSpell.ResetShards();
					if (availableSpell.NameKey == Spells.DrawTheBowstring)
					{
						availableSpell.chargeCount = 1uL;
					}
				}
			}
		};
		on_choose.Add(Spells.DrawTheBowstring, value);
		value2 = delegate
		{
			foreach (Spell availableSpell2 in AvailableSpells)
			{
				if (availableSpell2.Type == SpellTypeGroup.None)
				{
					availableSpell2.ResetShards();
				}
			}
			(GameManager.Instance.CurrentHero.HeroPanel.HeroMap[HeroesNames.Archer] as Archer).shooting.Tension.Reset();
		};
		on_clear.Add(Spells.DrawTheBowstring, value2);
		MemeticBanList.Add(Spells.DrawTheBowstring);
		list = new List<IEffect>
		{
			new ActionEffect(delegate
			{
				float num = (3.0 * GameManager.Instance.Orb.autoclicksFromSpell.Value.Pow(2.0)).Floor().ToFloat();
				GameManager.Instance.CurrentHero.ClassBonusStacks.Change(num);
				Orb orb = GameManager.Instance.Orb;
				orb.AutoClick(orb.click_profit.Value, orb.GetCritChange, orb.crit_profit.Value, 1f, pet_click: false, idle_break: false, 0f, num);
				(GameManager.Instance.CurrentHero.HeroPanel.HeroMap[HeroesNames.Archer] as Archer).shooting.Tension.Reset();
			}, null, null, 0f, isInstant: true, (string key) => (3.0 * GameManager.Instance.Orb.autoclicksFromSpell.Value.Pow(2.0)).Floor().ToReadableString("F0"))
		};
		effect_map.Add(Spells.TrainingShot, list);
		MemeticBanList.Add(Spells.TrainingShot);
		list = new List<IEffect>
		{
			new ActionEffect(delegate
			{
				float num = GameManager.Instance.Orb.autoclicksFromSpell.Value.Floor().ToFloat();
				ArcherShooting shooting = (GameManager.Instance.CurrentHero.HeroPanel.HeroMap[HeroesNames.Archer] as Archer).shooting;
				BigNumber amount = 10f * num * (1.0 + GameManager.Instance.Scrolls.SummoningEfficiency.Value);
				shooting.Focus.AddProgress(amount);
				GameManager.Instance.CurrentHero.ClassBonusStacks.Change(num);
				Orb orb = GameManager.Instance.Orb;
				orb.AutoClick(orb.click_profit.Value, orb.GetCritChange, orb.crit_profit.Value, 1f, pet_click: false, idle_break: false, 0f, num);
				shooting.Tension.Reset();
			}, null, null, 0f, isInstant: true, delegate(string key)
			{
				BigNumber bigNumber = GameManager.Instance.Orb.autoclicksFromSpell.Value.Floor();
				return (key == "t") ? bigNumber.ToReadableString("F0") : (10.0 * bigNumber * (1.0 + GameManager.Instance.Scrolls.SummoningEfficiency.Value)).ToReadableString();
			})
		};
		effect_map.Add(Spells.TracerShot, list);
		MemeticBanList.Add(Spells.TracerShot);
		list = new List<IEffect>
		{
			new ActionEffect(delegate
			{
				float num = GameManager.Instance.Orb.autoclicksFromSpell.Value.ToInt();
				ArcherShooting shooting = (GameManager.Instance.CurrentHero.HeroPanel.HeroMap[HeroesNames.Archer] as Archer).shooting;
				BigNumber amount = 1f * num * shooting.Accuracy.Value * (1.0 + GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity.Value);
				shooting.Vulnerable.AddProgress(amount);
				GameManager.Instance.CurrentHero.ClassBonusStacks.Change(num);
				Orb orb = GameManager.Instance.Orb;
				orb.AutoClick(orb.click_profit.Value, orb.GetCritChange, orb.crit_profit.Value, 1f, pet_click: false, idle_break: false, 0f, num);
				shooting.Tension.Reset();
			}, null, null, 0f, isInstant: true, delegate(string key)
			{
				BigNumber bigNumber = GameManager.Instance.Orb.autoclicksFromSpell.Value.ToInt();
				if (key == "t")
				{
					return bigNumber.ToReadableString("F0");
				}
				ArcherShooting shooting = (GameManager.Instance.CurrentHero.HeroPanel.HeroMap[HeroesNames.Archer] as Archer).shooting;
				return (1.0 * bigNumber * shooting.Accuracy.Value * (1.0 + GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity.Value)).ToReadableString();
			})
		};
		effect_map.Add(Spells.ExposingShot, list);
		MemeticBanList.Add(Spells.ExposingShot);
		list = new List<IEffect>
		{
			new ArcherBurst(1f)
		};
		effect_map.Add(Spells.DeadlyShot, list);
		MemeticBanList.Add(Spells.DeadlyShot);
		list = new List<IEffect>
		{
			new SimpleEffect(GameContext.GetResource("Archer.Accuracy"), 0.0, 1.2000000476837158, EffectNames.Linear, 0.5f)
		};
		effect_map.Add(Spells.InnerSight, list);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower, 0.10000000149011612, 1.5, GameManager.Instance.Scrolls.ShardsPassive, EffectNames.Pow)
		};
		effect_map.Add(Spells.SilverMoonlight, list);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Orb.crit_profit, 0.5, 1.0499999523162842, GetSpell(Spells.AimAtTheNeck).Use, EffectNames.Pow, 1.5f)
		};
		effect_map.Add(Spells.AimAtTheNeck, list);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Orb.autoclick_profit, 0.5, 1.0499999523162842, GetSpell(Spells.BlessedArrow).Use, EffectNames.Pow, 1.5f)
		};
		effect_map.Add(Spells.BlessedArrow, list);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Profit, 0.0, 0.5, GameManager.Instance.BuildingManager.GetBuilding(5).TotalLevel, EffectNames.Linear, 2f)
		};
		effect_map.Add(Spells.ManaInfusion, list);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 10.0, 1.5, GetSpell(Spells.MightyBow).Use, EffectNames.Pow, 0.5f)
		};
		effect_map.Add(Spells.MightyBow, list);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Orb.autoclicksFromSpell, 9.999999747378752E-05, 0.699999988079071, GetSpell(Spells.Multishot).Use, EffectNames.Pow, 0f)
		};
		effect_map.Add(Spells.Multishot, list);
		NonscaledSpells.Add(Spells.Multishot);
		MemeticBanList.Add(Spells.Multishot);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.CurrentPet.AbilityPower, 0.0010000000474974513, 0.5, GameContext.GetResource("Archer.Accuracy"), EffectNames.Pow, 0.25f)
		};
		effect_map.Add(Spells.Fetch, list);
		NonscaledSpells.Add(Spells.Fetch);
		MemeticBanList.Add(Spells.Fetch);
		persistentPassive = new Dictionary<Spells, IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentPet.ExpBonus;
		simpleEffect.add = 0.019999999552965164;
		simpleEffect.mult = 0.0;
		simpleEffect.parameter = GetSpell(Spells.RulesOfNature).Use;
		persistentPassive.Add(Spells.RulesOfNature, simpleEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.SpellChargingSpeed, 0.0010000000474974513, 0.5, GetSpell(Spells.Nightfall).Use, EffectNames.Pow);
		persistentPassive.Add(Spells.Nightfall, simpleEffect);
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.target = GameManager.Instance.CurrentHero.ExpManaSources;
		simpleEffect.add = 0.001;
		simpleEffect.mult = 0.3499999940395355;
		simpleEffect.parameter = GetSpell(Spells.TrueSorcery).Use;
		persistentPassive.Add(Spells.TrueSorcery, simpleEffect);
		persistentPassive.Add(Spells.JAMissileStorm, new EffectJMS(0.05f, 0.8f, 1f, 0.01f, Spells.JAMissileStorm));
		persistentPassive.Add(Spells.TemperTheSteel, new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower, 0.0, 0.009999999776482582, GetSpell(Spells.TemperTheSteel).Use));
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.ShardsPassive, 0.0, 0.20000000298023224);
		simpleEffect.parameter = GetSpell(Spells.MaterializeCosmicConduit).Use;
		simpleEffect.pow_diminishing = 0.5f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.MaterializeCosmicConduit, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentPet.AbilityPower, 0.02, 1.25, EffectNames.Pow);
		simpleEffect.parameter = GetSpell(Spells.ForgeEmpyrealVassal).Use;
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ForgeEmpyrealVassal, list);
		NonscaledSpells.Add(Spells.ForgeEmpyrealVassal);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower, 0.5, 0.75, EffectNames.PowA);
		simpleEffect.parameter = GetSpell(Spells.ManifestTwistedReality).Use;
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.Idle.IdleBonus, 0.5, 0.75, EffectNames.PowA);
		simpleEffect.parameter = GetSpell(Spells.ManifestTwistedReality).Use;
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ManifestTwistedReality, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 0.0, 0.20000000298023224);
		simpleEffect.parameter = GetSpell(Spells.EmployCelestialCountermeasures).Use;
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.SummoningEfficiency, 0.0010000000474974513, 0.4399999976158142, EffectNames.Pow);
		simpleEffect.parameter = GetSpell(Spells.EmployCelestialCountermeasures).Use;
		simpleEffect.diminishing = 0f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.EmployCelestialCountermeasures, list);
		NonscaledSpells.Add(Spells.EmployCelestialCountermeasures);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.EvocationEfficiency;
		simpleEffect.mult = 0.02800000086426735;
		simpleEffect.parameter = GameManager.Instance.CurrentHero.Level;
		simpleEffect.pow_diminishing = 1.2f;
		list.Add(simpleEffect);
		item5 = new ActionEffect(null, null, delegate
		{
			if (GameManager.Instance.CurrentHero.Hero.SpellList.Contains(Spells.PlagueZombie2))
			{
				Spell spell = GameManager.Instance.SpellBook.GetSpell(Spells.PlagueZombie2);
				float value3 = 200f / (float)(spell.chargeCount + 1);
				value3 = Mathf.Clamp(value3, 0f, 100f) / 100f * UnityEngine.Random.Range(0.2f, 1.8f);
				spell.AddProgressBuild(value3, shards: false);
			}
		}, 1f);
		list.Add(item5);
		effect_map.Add(Spells.VoraciousPlague2, list);
		list = new List<IEffect>();
		list.Add(new MultiChargesEffect(100f, Spells.PlagueZombie2, 0.1f, 2.5f));
		effect_map.Add(Spells.PlagueZombie2, list);
		list = new List<IEffect>();
		effectUseMM = new EffectUseMMBulk(0.1f, Spells.JAMissileStorm2);
		effectUseMM.base_count = 10;
		effectUseMM.add = 1000.0;
		effectUseMM.mult = 1.3f;
		effectUseMM.parameter = GetSpell(Spells.JAMissileStorm2).UseThisRun;
		list.Add(effectUseMM);
		effect_map.Add(Spells.JAMissileStorm2, list);
		NonscaledSpells.Add(Spells.JAMissileStorm2);
		persistentPassive.Add(Spells.JAMissileStorm2, new EffectJMS(0.05f, 0.8f, 1f, 0.01f, Spells.JAMissileStorm2));
		list = new List<IEffect>();
		list.Add(new EffectAddProductionPeriodic(10f, 1f, 0.0, 0.10000000149011612, GetSpell(Spells.HellStorm2).Use));
		item5 = new ActionEffect(null, null, delegate(BigNumber eff)
		{
			if (GameManager.Instance.CurrentPet.Pet != null)
			{
				GameManager.Instance.CurrentPet.Pet.AddExp(GameManager.Instance.CurrentHero.Level.ValueInt * (1.0 + eff.Pow(1.5)));
			}
		}, 1f);
		list.Add(item5);
		effect_map.Add(Spells.HellStorm2, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(1f, 1f, 1f, 1f, crit_const: false, 1f, fromSpell: false);
		item2.pow_diminishing = 1.5f;
		list.Add(new EffectAutoclickVariable(15f, 1f, GameManager.Instance.CurrentHero.ClassBonusStacks, item2, useLog: false, fromSpell: true));
		effect_map.Add(Spells.TemplarBrothers2, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(5f));
		list.Add(new SimpleEffect(GameManager.Instance.Orb.crit_profit, 0.0, 1.1));
		effect_map.Add(Spells.DivineAlly2, list);
		list = new List<IEffect>
		{
			new EffectAutoClick(20f, 3f),
			new SimpleEffect(GameManager.Instance.Orb.critRating.crit_rating, 0.0, 0.3499999940395355, GameManager.Instance.CurrentHero.Level)
		};
		effect_map.Add(Spells.SpiritOfValor2, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(16f, 1f, 10f, 9f, crit_const: true);
		list.Add(item2);
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.ShardsPassive, 0.0, 2.0);
		simpleEffect.diminishing = 0.1f;
		list.Add(simpleEffect);
		list.Add(new SimpleEffect(GameManager.Instance.Orb.click_profit, 0.0, 2.0));
		effect_map.Add(Spells.ConjureManabeast2, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Profit;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 1.5;
		simpleEffect.parameter = GetSpell(Spells.TimeHelix2).Use;
		simpleEffect.diminishing = 0.95f;
		EffectTimeHelixQ effectTimeHelixQ = new EffectTimeHelixQ(simpleEffect);
		list.Add(effectTimeHelixQ);
		effect_map.Add(Spells.TimeHelix2, list);
		on_choose.Add(Spells.TimeHelix2, effectTimeHelixQ.OnSelect);
		on_clear.Add(Spells.TimeHelix2, effectTimeHelixQ.OnDeselect);
		list = new List<IEffect>();
		list.Add(new EffectUmbralRageQR(GameContext.GetEffect("PowW"), GameManager.Instance.Scrolls.EvocationEfficiency, 1.2000000476837158, 1.0));
		effect_map.Add(Spells.UmbralRage2, list);
		on_choose.Add(Spells.UmbralRage2, delegate
		{
			add_charge_shadows(Spells.UmbralRage2);
		});
		on_clear.Add(Spells.UmbralRage2, delegate
		{
			remove_charge_shadows(Spells.UmbralRage2);
		});
		list = new List<IEffect>();
		list.Add(new EffectDancingFlame(5f, 1f, 1f, 1f, crit_const: false, 1f, null, 300f, fromSpell: true, 0.1f));
		effect_map.Add(Spells.DancingFlame2, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(5f, 1f, -1f, 0f, crit_const: true));
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("PowA");
		simpleEffect.target = GameContext.GetResource("Shadow.IncomeMod");
		simpleEffect.add = 1.0;
		simpleEffect.mult = 1.0;
		simpleEffect.pow_diminishing = 0.1f;
		simpleEffect.parameter = GetSpell(Spells.ConjureLesserElemental2).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.ConjureLesserElemental2, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(10f, 1f, 5f, 7.5f, crit_const: true));
		combineEffect = new CombineEffect(GameContext.GetResource("Shadow.IncomeMod"));
		combineEffect.AddEffect(1.0, 1.0, GameContext.GetResource("Shadow.SpawnSpeed"), GameContext.GetEffect(EffectNames.Pow.ToString()), eff: true, 1f, 0.1f);
		combineEffect.AddEffect(1.0, 1.0, GameContext.GetResource("Shadow.LifeTime"), GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.ConjureGreaterElemental2, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(15f, 1f, 10f, 12f, crit_const: true));
		list.Add(new SimpleEffect(GameContext.GetResource("Shadow.IncomeMod"), 1.2000000476837158, 0.10000000149011612, GameManager.Instance.Idle.IdleBonus, EffectNames.Pow, 0.1f));
		effect_map.Add(Spells.ConjurePrimalElemental2, list);
		list = new List<IEffect>();
		list.Add(new ManaPotCharge(PotKey.Mana));
		effect_map.Add(Spells.QPotMana, list);
		MemeticBanList.Add(Spells.QPotMana);
		list = new List<IEffect>();
		list.Add(new VManaPotCharge(PotKey.Void));
		effect_map.Add(Spells.QPotVMana, list);
		MemeticBanList.Add(Spells.QPotVMana);
		list = new List<IEffect>();
		list.Add(new ProdPotCharge(PotKey.Production));
		effect_map.Add(Spells.QPotProd, list);
		MemeticBanList.Add(Spells.QPotProd);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 1.0, 1.0, EffectNames.Pow);
		simpleEffect.parameter = GetSpell(Spells.QAlchVPE).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.QAlchVPE, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 1.0, 1.0, EffectNames.Pow);
		simpleEffect.parameter = GetSpell(Spells.QAlchEvo).Use;
		list.Add(simpleEffect);
		effect_map.Add(Spells.QAlchEvo, list);
		list = new List<IEffect>();
		item5 = new ActionEffect(null, null, delegate
		{
			AboAttributePanel aboAttributes = (GameManager.Instance.Realmcraft.Get("abo") as Planes.Abolisher).aboAttributes;
			BigNumber exp = 1.0 + (1.0 + (1.0 + Statistic.AutoClicks.Value / "5e5").Log10()) * (1.0 + (1.0 + Statistic.CastSpell.Value / "1e5").Log10()) * (1.0 + (1.0 + Statistic.ClickableCollect.Value / "1e4").Log10()) * (1.0 + GameManager.Instance.Scrolls.SummoningEfficiency.Value.Log10());
			BigNumber bigNumber = GameManager.Instance.CurrentHero.Level.Value - 100.0;
			if (bigNumber > 0.0)
			{
				exp *= 1.0 + bigNumber / 50.0;
			}
			aboAttributes.AddExp(exp);
		}, 1f);
		list.Add(item5);
		effect_map.Add(Spells.CorporealForm, list);
		MemeticBanList.Add(Spells.CorporealForm);
		list = new List<IEffect>();
		list.Add(new HCToCasts(1f));
		effect_map.Add(Spells.SummHCToCasts, list);
		NonscaledSpells.Add(Spells.SummHCToCasts);
		list = new List<IEffect>();
		list.Add(new HCCap(1.0, 1.100000023841858, GetSpell(Spells.HCCapAug).Use));
		effect_map.Add(Spells.HCCapAug, list);
		NonscaledSpells.Add(Spells.HCCapAug);
		list = new List<IEffect>();
		list.Add(new HCGain(1, 1.0, 1.0, GetSpell(Spells.HCGain).UseThisRun));
		effect_map.Add(Spells.HCGain, list);
		persistentPassive.Add(Spells.HCGain, new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower, 1.0, 2.0, GetSpell(Spells.HCGain).Use, EffectNames.Pow));
		offline.Add(Spells.HCGain);
		list = new List<IEffect>();
		list.Add(new IronSoulPOSEffect(2f));
		effect_map.Add(Spells.PowerOfSacrifice2, list);
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect(GameManager.Instance.Orb.critRating.crit_rating, 0.0, 1.0, GameManager.Instance.BuildingManager.GetBuilding(3).TotalLevel);
		list.Add(simpleEffect);
		effect_map.Add(Spells.MonumentsToCritRating, list);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed, 0.0, 2.0),
			new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.BonusLifeTime, 0.0, 4.0, EffectNames.Linear, 0.7f)
		};
		combineEffect = new CombineEffect(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity);
		combineEffect.AddEffect(1.0, 0.05000000074505806, GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: true, 0.5f);
		combineEffect.AddEffect(1.0, 0.05000000074505806, GameManager.Instance.VoidManaManager.VoidCore.BonusLifeTime, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.EVoidLure, list);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Profit);
		combineEffect.AddEffect(1.0, 0.550000011920929, GetSpell(Spells.ERoP).Use, GameContext.GetEffect(EffectNames.Pow.ToString()));
		combineEffect.AddEffect(1.0, 0.10000000149011612, GameManager.Instance.Scrolls.EvoCastCount, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.ERoP, list);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Orb.autoclick_profit);
		combineEffect.AddEffect(0.0, 0.20000000298023224, GetSpell(Spells.ERoN).UseThisRun, GameContext.GetEffect(EffectNames.Linear.ToString()));
		combineEffect.AddEffect(1.0, 0.20000000298023224, GameManager.Instance.Scrolls.SummoningEfficiency, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.ERoN, list);
		Action<float> rules_of_nature_crit_e = delegate(float amount)
		{
			GameManager.Instance.Scrolls.FillScroll(Spells.ERoN, amount);
		};
		value = delegate
		{
			Orb orb = GameManager.Instance.Orb;
			orb.OnCrit = (Action<float>)Delegate.Combine(orb.OnCrit, rules_of_nature_crit_e);
		};
		on_choose.Add(Spells.ERoN, value);
		value2 = delegate
		{
			Orb orb = GameManager.Instance.Orb;
			orb.OnCrit = (Action<float>)Delegate.Remove(orb.OnCrit, rules_of_nature_crit_e);
		};
		on_clear.Add(Spells.ERoN, value2);
		persistentPassive.Add(Spells.ERoN, new SimpleEffect(GameManager.Instance.CurrentPet.ExpBonus, 0.10000000149011612, 1.100000023841858, GetSpell(Spells.ERoN).Use, EffectNames.AdditivePow));
		list = new List<IEffect>
		{
			new EffectAddProduction(66f, 0.0, 0.0),
			new SimpleEffect(GameManager.Instance.CurrentPet.ExpBonus, 1.0, 2.0, Statistic.AutoClicks, EffectNames.Log10, 0.05f)
		};
		item2 = new EffectAutoClick(26f);
		item2.diminishing = 0.1f;
		item2.PetClick = true;
		list.Add(item2);
		effect_map.Add(Spells.EFireBall, list);
		list = new List<IEffect>();
		effectUseMM = new EffectUseMMBulk(0.2f, Spells.EJMS, triggerPassive: true);
		effectUseMM.base_count = 100;
		effectUseMM.add = 1000.0;
		effectUseMM.mult = 1.35f;
		effectUseMM.parameter = GetSpell(Spells.EJMS).UseThisRun;
		list.Add(effectUseMM);
		effect_map.Add(Spells.EJMS, list);
		NonscaledSpells.Add(Spells.EJMS);
		persistentPassive.Add(Spells.EJMS, new EffectJMS(0.05f, 0.8f, 1f, 0.01f, Spells.EJMS));
		list = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		combineEffect = new CombineEffect(GameManager.Instance.Profit);
		combineEffect.AddEffect(0.800000011920929, 0.8999999761581421, GetSpell(Spells.ENightfall).UseThisRun, GameContext.GetEffect(EffectNames.PowA.ToString()));
		combineEffect.AddEffect(1.0, 0.10000000149011612, GameManager.Instance.Idle.IdleBonus, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.ENightfall, list);
		Action<Spell> enightfall_oncast = delegate(Spell s)
		{
			if (s.NameKey != Spells.Nightfall)
			{
				GameManager.Instance.Scrolls.FillScroll(Spells.ENightfall, 1.0);
			}
		};
		Action<float> enightfall = delegate(float t)
		{
			float num = Mathf.Clamp(((GameManager.Instance.CurrentHero.PlayedTime.ValueInt + GameManager.Instance.CurrentHero.SkipedPlayedTime.Value) / 3600.0).ToFloat(), 1f, 150f);
			num /= 5f;
			GameManager.Instance.Scrolls.FillScroll(Spells.ENightfall, t * num);
		};
		value = delegate
		{
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Combine(instance.GameTick, enightfall);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, enightfall_oncast);
		};
		on_choose.Add(Spells.ENightfall, value);
		value2 = delegate
		{
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Remove(instance.GameTick, enightfall);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, enightfall_oncast);
		};
		on_clear.Add(Spells.ENightfall, value2);
		simpleEffect = new SimpleEffect(GameManager.Instance.Scrolls.SpellChargingSpeed, 0.0020000000949949026, 0.5, GetSpell(Spells.ENightfall).Use, EffectNames.Pow);
		persistentPassive.Add(Spells.ENightfall, simpleEffect);
		list = new List<IEffect>
		{
			new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 0.5, 2.0, GameManager.Instance.CurrentHero.Level, EffectNames.PowA),
			new SimpleEffect(GameManager.Instance.Orb.autoclicksFromSpell, 0.0, 0.014999999664723873, GameManager.Instance.CurrentHero.Level, EffectNames.Linear, 0f)
		};
		effect_map.Add(Spells.EPrimalPower, list);
		list = new List<IEffect>();
		list.Add(new EffectAutoClick(12f));
		combineEffect = new CombineEffect(GameManager.Instance.VoidManaManager.Power);
		combineEffect.AddEffect(1.0, 0.4000000059604645, Statistic.MaxVoidManaSession, GameContext.GetEffect(EffectNames.PowA.ToString()), eff: true, 1f, 0.65f);
		combineEffect.AddEffect(0.0, 0.10000000149011612, GetSpell(Spells.EVoidElemental).Use, GameContext.GetEffect(EffectNames.Linear.ToString()), eff: false);
		list.Add(combineEffect);
		simpleEffect = new SimpleEffect(GameManager.Instance.Profit, 1.0, 0.10000000149011612, GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, EffectNames.Pow);
		simpleEffect.diminishing = 0.5f;
		list.Add(simpleEffect);
		effect_map.Add(Spells.EVoidElemental, list);
		list = new List<IEffect>();
		item2 = new EffectAutoClick(30f, 5f);
		list.Add(item2);
		combineEffect = new CombineEffect(GameManager.Instance.Orb.critRating.crit_rating);
		combineEffect.AddEffect(0.0, 1.5, GameManager.Instance.CurrentHero.Level, GameContext.GetEffect(EffectNames.Linear.ToString()));
		combineEffect.AddEffect(1.0, 1.5, GameManager.Instance.Scrolls.SummoningEfficiency, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		effect_map.Add(Spells.ESpiritOfValor, list);
		list = new List<IEffect>();
		combineEffect = new CombineEffect(GameManager.Instance.Scrolls.EvocationEfficiency);
		combineEffect.AddEffect(5.0, 1.0, GetSpell(Spells.ENullZone).Use, GameContext.GetEffect(EffectNames.Pow.ToString()));
		combineEffect.AddEffect(1.5, 1.0, GameManager.Instance.BuildingManager.GetBuilding(6).TotalLevel, GameContext.GetEffect(EffectNames.Pow.ToString()), eff: false);
		list.Add(combineEffect);
		list.Add(new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 0.0, 2.0, EffectNames.Linear, 0.1f));
		effect_map.Add(Spells.ENullZone, list);
		list = new List<IEffect>
		{
			new EffectStabilizeTheFlow(new SimpleEffect(GameManager.Instance.Profit, 0.0, 10.0), 1.2f)
		};
		effect_map.Add(Spells.EStabilizeTheFlow, list);
		list = new List<IEffect>();
		list.Add(new SimpleEffect(GameContext.GetResource("Building.1.Profit"), 0.0024999999441206455, 2.5, GameManager.Instance.BuildingManager.GetBuilding(1).TotalLevel, EffectNames.PowA));
		effect_map.Add(Spells.ECriticalMass, list);
		on_choose.Add(Spells.ECriticalMass, delegate
		{
			add_charge_shadows(Spells.ECriticalMass);
		});
		on_clear.Add(Spells.ECriticalMass, delegate
		{
			remove_charge_shadows(Spells.ECriticalMass);
		});
		list = new List<IEffect>
		{
			new EffectAutoClick(10f),
			new SimpleEffect(Reborn.SoulPower, 0.1, 0.5, Statistic.ResourcesCollected, EffectNames.Pow, 0.5f),
			new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 0.014999999664723873, 0.5, Statistic.CastSpell, EffectNames.PowA, 0.8f)
		};
		effect_map.Add(Spells.EArtificialMuse, list);
		list = new List<IEffect>();
		list.Add(new EffectTemperTheSteelPeriodic(1f, 0.05f, 0.001f, 0f, GetSpell(Spells.ETTS).UseThisRun, 0.9f, Spells.ETTS));
		list.Add(new EffectTemperTheSteelPersistent(Spells.ETTS));
		effect_map.Add(Spells.ETTS, list);
		persistentPassive.Add(Spells.ETTS, new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower, 0.01, 1.5, GetSpell(Spells.ETTS).Use, EffectNames.PowA));
		MemeticBanList.Add(Spells.ETTS);
	}
}
