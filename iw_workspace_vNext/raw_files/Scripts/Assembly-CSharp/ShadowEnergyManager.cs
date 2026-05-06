using System;
using System.Collections.Generic;
using UnityEngine;

public class ShadowEnergyManager : MonoBehaviour
{
	public static ShadowEnergyManager instance;

	public Sprite icon;

	private Sprite voidIcon;

	private GameManagerVisual manager;

	private bool activated;

	public BonusBehavior ShadowCore;

	public VariableBignumber ShadowEnergy;

	public VariableComplex PassiveIncome;

	public VariableComplex IncomeMod;

	public List<Spells> spells;

	public VariableFloat Period;

	public VariableComplex PartToSpell;

	private float timer;

	private List<Scroll> scrolls;

	public Action<BigNumber> OnSpend;

	public void Init()
	{
		instance = this;
		ShadowEnergy = new VariableBignumber(0.0);
		VariableBignumber shadowEnergy = ShadowEnergy;
		shadowEnergy.OnChangeAdd = (Action<BigNumber>)Delegate.Combine(shadowEnergy.OnChangeAdd, new Action<BigNumber>(OnGetSE));
		PassiveIncome = new VariableComplex(0.0);
		IncomeMod = new VariableComplex(1.0);
		Period = new VariableFloat(5f);
		PartToSpell = new VariableComplex(0.019999999552965164);
		scrolls = GameManager.Instance.Scrolls.Scrolls;
		ShadowCore.Init(AddShadow, IncomeMod);
		spells = new List<Spells>();
		GameContext.ContextAddResource("Shadow.ShadowEnergy", ShadowEnergy);
		GameContext.ContextAddResource("Shadow.PassiveIncome", PassiveIncome);
		GameContext.ContextAddResource("Shadow.IncomeMod", IncomeMod);
		GameContext.ContextAddResource("Shadow.LifeTime", ShadowCore.BonusLifeTime);
		GameContext.ContextAddResource("Shadow.SpawnSpeed", ShadowCore.SpawnSpeed);
		GameContext.ContextAddResource("Shadow.BonusIncome", ShadowCore.AmountPerEntity);
		GameContext.ContextAddResource("Shadow.Period", Period);
		GameContext.ContextAddResource("Shadow.Charging", PartToSpell);
	}

	public void Activate()
	{
		GameManager.Instance.BonusSpawner.DisableAll();
		GameManager.Instance.VoidManaManager.VoidCore.SpawnRate = 0f;
		GameManager.Instance.VoidManaManager.Income.SetValue(0.0);
		GameManager.Instance.VoidMana.SetValue(0.0);
		if (!activated)
		{
			if (manager == null)
			{
				manager = UnityEngine.Object.FindObjectOfType<GameManagerVisual>();
			}
			voidIcon = manager.VoidIcon.sprite;
			manager.VoidIcon.sprite = icon;
			PassiveIncome.SetValue(0.0);
			base.gameObject.SetActive(value: true);
			GameManager.Instance.BonusSpawner.Init(ShadowCore);
			manager.EnableShadows(this);
			activated = true;
		}
	}

	public double GetEnergy(BigNumber cost)
	{
		if (ShadowEnergy.Value > cost)
		{
			ShadowEnergy.Change(-cost);
			OnSpend?.Invoke(cost);
			return cost.ToDouble();
		}
		BigNumber value = ShadowEnergy.Value;
		ShadowEnergy.SetValue(0.0);
		OnSpend?.Invoke(value);
		return value.ToDouble();
	}

	public void Update()
	{
		AddShadow(PassiveIncome.Value * IncomeMod.Value * Time.deltaTime);
		if (timer >= Period.ValueFloat)
		{
			if (spells.Count > 0)
			{
				for (int i = 0; i < scrolls.Count; i++)
				{
					if (scrolls[i].spell != null && spells.Contains(scrolls[i].spell.NameKey))
					{
						BigNumber bigNumber = ShadowEnergy.Value * PartToSpell.Value;
						BigNumber bigNumber2 = scrolls[i].GetSpellShardCapacity();
						if (bigNumber > bigNumber2)
						{
							bigNumber = bigNumber2;
						}
						if (bigNumber != 0.0)
						{
							scrolls[i].AddProgressBuild(GetEnergy(bigNumber), text: false, shards: false);
						}
					}
				}
			}
			timer -= Period.ValueFloat;
		}
		timer += Time.deltaTime;
	}

	public void Deactivate()
	{
		GameManager.Instance.VoidManaManager.VoidCore.SpawnRate = 1f;
		GameManager.Instance.VoidManaManager.Income.SetValue(1.0);
		if (activated)
		{
			manager.VoidIcon.sprite = voidIcon;
			base.gameObject.SetActive(value: false);
			GameManager.Instance.BonusSpawner.Init(GameManager.Instance.VoidManaManager.VoidCore);
			manager.DisableShadows();
			ShadowEnergy.SetValue(0.0);
			activated = false;
		}
	}

	public void AddShadow(BigNumber value, int count = 1)
	{
		if (BigNumber.Sign(value) >= 1)
		{
			ShadowEnergy.Change(value * count);
			GameManager.Instance.BonusSpawner.OnGetResource?.Invoke();
		}
	}

	private void OnGetSE(BigNumber add)
	{
		if (BigNumber.Sign(add) == 1)
		{
			Statistic.Change(Statistic.LSTotal, add);
		}
	}

	public bool isActivated()
	{
		return activated;
	}
}
