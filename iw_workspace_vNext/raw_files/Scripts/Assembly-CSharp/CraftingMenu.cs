using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMenu : MonoBehaviour
{
	public CreateNew createNew;

	public GameObject experiments;

	public DustGambling gambling;

	public ExperimentDust dust;

	public ExperimentBuff buff;

	public ExperimentCatas catas;

	public ExperimentConsumables consumables;

	public ExperimentCurrencies currencies;

	public ExperimentNullifier nullifier;

	public ExperimentRune runeProgress;

	public ExperimentKeys keys;

	public ExperimentGilding gilding;

	public ExperimentProficiency proficiency;

	public CraftingLog log;

	public CraftingRune rune;

	public List<DustGambling.SpriteMap> sprites;

	public Action OnExperiment;

	public Action<Dictionary<CraftResource, BigNumber>> OnExperimentCost;

	public VariableFloat edustConvert;

	public VariableInt experimetCounter;

	public VariableInt experimetCounterRealm;

	[SerializeField]
	private Toggle experimentAllIn;

	private float performedCount;

	[SerializeField]
	private TextMeshProUGUI performedLabel;

	private CraftManager h_manager;

	private CraftManager manager
	{
		get
		{
			if (h_manager == null)
			{
				h_manager = GameManager.Instance.Craft;
			}
			return h_manager;
		}
	}

	private void OnEnable()
	{
		experimentAllIn.isOn = false;
		performedCount = 0f;
		performedLabel.text = performedCount.ToString();
	}

	public void Init(CraftInvestment invest)
	{
		edustConvert = new VariableFloat(0f);
		experimetCounter = new VariableInt(0);
		experimetCounterRealm = new VariableInt(0);
		sprites = gambling.sprites;
		createNew.investment = invest;
		dust.Init(this, 1111);
		buff.Init(this, 0);
		catas.Init(this, 2113);
		nullifier.Init(this, 3576);
		runeProgress.Init(this, 4197);
		keys.Init(this, 5835);
		gilding.Init(this, 8731);
		proficiency.Init(this, 9954);
		GameContext.ContextAddResource("Gamble.AddEDust", edustConvert);
		GameContext.ContextAddResource("Experiment.Total", experimetCounter);
		GameContext.ContextAddResource("Experiment.Realm", experimetCounterRealm);
	}

	public Sprite GetSprite(DustGambling.DropType type, int id = 0)
	{
		return sprites.Find((DustGambling.SpriteMap x) => x.type == type && x.id == id).sprite;
	}

	public void Invest()
	{
		if (manager.DropList.Count > 0)
		{
			manager.AddProgress2New(createNew.GetSlotFromFilter());
			createNew.Invest();
			UpdateLabels();
		}
	}

	public void UpdateLabels()
	{
		manager.window.SkillLabels();
		performedLabel.text = performedCount.ToString();
	}

	public void Refresh()
	{
		UpdateCreateNew();
		experiments.SetActive(GameManager.Instance.Paragon.EnchantingIsAvailable);
		keys.gameObject.SetActive(GameManager.Instance.Paragon.LocationsIsAvailable);
		gilding.gameObject.SetActive(GameManager.Instance.Paragon.GildingIsAvailable);
		proficiency.gameObject.SetActive(GameManager.Instance.Paragon.ProficiencyIsAvailable);
	}

	public void UpdateCreateNew()
	{
		createNew.UpdateLabels();
		createNew.gameObject.SetActive(GameManager.Instance.Craft.DropList.Count > 0);
	}

	public bool IsAllIn()
	{
		return experimentAllIn.isOn;
	}

	public void OnChangeToogle()
	{
		if (!experimentAllIn.isOn)
		{
			StopAll();
		}
	}

	private void StopAll()
	{
		dust.StopAll();
		buff.StopAll();
		catas.StopAll();
		consumables.StopAll();
		keys.StopAll();
		currencies.StopAll();
		gilding.StopAll();
		proficiency.StopAll();
		runeProgress.StopAll();
		nullifier.StopAll();
	}

	public void Experiment(Dictionary<CraftResource, BigNumber> cost, bool runeProgress)
	{
		BigNumber bigNumber = 0.0;
		CraftManager craft = GameManager.Instance.Craft;
		foreach (KeyValuePair<CraftResource, BigNumber> item in cost)
		{
			craft.map[item.Key].Change(-item.Value);
			bigNumber += item.Value;
		}
		craft.Crafting.AddExp(bigNumber.ToFloat() / 4800f);
		if (runeProgress)
		{
			rune.AddProgress(bigNumber);
		}
		if (edustConvert.ValueFloat >= 1f)
		{
			BigNumber bigNumber2 = bigNumber * (edustConvert.ValueFloat / 100f);
			bigNumber2 *= GameManager.Instance.Resources.GetEnchantingDustIncome();
			craft.GiveEnchantingDust(bigNumber2);
			log.Throw(GetSprite(DustGambling.DropType.EnchantingDust), bigNumber2);
		}
		performedCount += 1f;
		UpdateLabels();
		experimetCounter.Change(1);
		experimetCounterRealm.Change(1);
		if (OnExperiment != null)
		{
			OnExperiment();
		}
		if (OnExperimentCost != null)
		{
			OnExperimentCost(cost);
		}
	}

	public GamblingToSave Save()
	{
		return new GamblingToSave
		{
			d = dust.GetTries(),
			b = buff.GetTries(),
			ca = catas.GetTries(),
			nu = nullifier.GetTries(),
			rp = runeProgress.GetTries(),
			g = gilding.GetTries(),
			k = keys.GetTries(),
			p = proficiency.GetTries(),
			r = rune.GetProgress(),
			realm = experimetCounterRealm.ValueInt
		};
	}

	public void Load(GamblingToSave save)
	{
		if (save == null)
		{
			dust.Load(0);
			buff.Load(0);
			currencies.Load(0);
			catas.Load(0);
			runeProgress.Load(0);
			nullifier.Load(0);
			gilding.Load(0);
			keys.Load(0);
			proficiency.Load(0);
			rune.LoadProgress(0f);
			experimetCounter.SetValue(0);
			experimetCounterRealm.SetValue(0);
		}
		else
		{
			dust.Load(save.d);
			buff.Load(save.b);
			catas.Load(save.ca);
			nullifier.Load(save.nu);
			runeProgress.Load(save.rp);
			gilding.Load(save.g);
			keys.Load(save.k);
			proficiency.Load(save.p);
			rune.LoadProgress(save.r);
			int value = save.d + save.b + save.cu + save.ca + save.co + save.g + save.k;
			experimetCounter.SetValue(value);
			experimetCounterRealm.SetValue(save.realm);
		}
	}
}
