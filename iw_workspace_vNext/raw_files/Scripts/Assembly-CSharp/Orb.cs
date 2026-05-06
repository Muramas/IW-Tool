using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Orb : MonoBehaviour
{
	private class AutoclickPool
	{
		public class AutoclickData
		{
			public BigNumber Amount;

			public BigNumber Mana;

			public BigNumber Shards;

			public BigNumber PetExp;

			public BigNumber IsCritical;

			public AutoclickData(BigNumber crit, BigNumber mana)
			{
				Amount = 0.0;
				IsCritical = crit;
				Mana = mana;
				Shards = 0.0;
				PetExp = 0.0;
			}
		}

		public Dictionary<string, AutoclickData> data;

		public IncomeCounter AutoclickCounter;

		public double autoclickPerSec;

		public double tempAC;

		public float timerAC;

		private float timerRelease;

		public AutoclickPool()
		{
			Reset();
			AutoclickCounter = new IncomeCounter(20);
		}

		public void Update(float dt)
		{
			timerAC += dt;
			if (timerAC >= 1f)
			{
				autoclickPerSec = tempAC;
				timerAC -= 1f;
				tempAC = 0.0;
			}
			timerRelease += dt;
			if (timerRelease > 0.25f)
			{
				timerRelease = 0f;
				Release();
			}
		}

		public void Reset()
		{
			data = new Dictionary<string, AutoclickData>();
		}

		public void Release()
		{
			BigNumber bigNumber = 0.0;
			BigNumber bigNumber2 = 0.0;
			BigNumber crit = 0.0;
			foreach (KeyValuePair<string, AutoclickData> datum in data)
			{
				BigNumber bigNumber3 = datum.Value.Mana * datum.Value.Amount;
				crit += datum.Value.IsCritical;
				bigNumber += bigNumber3;
				bigNumber2 += datum.Value.Amount;
				GameManager.Instance.ManaChange(bigNumber3);
				GameManager.Instance.Orb.AutoclickCounter.Add(bigNumber3);
				if (datum.Value.Shards >= 1.0)
				{
					GameManager.Instance.Orb.DropShards(datum.Value.Shards);
				}
				if (datum.Value.PetExp >= 1.0 && GameManager.Instance.CurrentPet.Pet != null)
				{
					GameManager.Instance.CurrentPet.Pet.AddExpConst(datum.Value.PetExp);
				}
			}
			if (bigNumber2 >= 1.0)
			{
				AutoclickCounter.Add(bigNumber / bigNumber2);
				GameManager.Instance.Orb.text_up(bigNumber, crit, auto: true, bigNumber2);
			}
			Reset();
		}

		public void ReleaseMin()
		{
			string text = string.Empty;
			BigNumber bigNumber = 0.0;
			foreach (KeyValuePair<string, AutoclickData> datum in data)
			{
				if (text == string.Empty || datum.Value.Amount < bigNumber)
				{
					text = datum.Key;
					bigNumber = datum.Value.Amount;
				}
			}
			AutoclickData autoclickData = data[text];
			GameManager.Instance.Orb.text_up(text, autoclickData.IsCritical, auto: true, autoclickData.Amount);
			BigNumber bigNumber2 = autoclickData.Mana * autoclickData.Amount;
			GameManager.Instance.ManaChange(bigNumber2);
			GameManager.Instance.Orb.AutoclickCounter.Add(bigNumber2);
			if (autoclickData.Shards >= 1.0)
			{
				GameManager.Instance.Orb.DropShards(autoclickData.Shards);
			}
			if (autoclickData.PetExp >= 1.0)
			{
				GameManager.Instance.CurrentPet.Pet.AddExp(autoclickData.PetExp);
			}
			AutoclickCounter.Add(bigNumber2 / autoclickData.Amount);
			Reset();
		}

		public void Add(BigNumber profit, BigNumber shards, BigNumber exp, BigNumber crit, double times = 1.0)
		{
			string key = profit.ToScientific();
			if (!data.ContainsKey(key))
			{
				data.Add(key, new AutoclickData(0.0, profit));
			}
			AutoclickData autoclickData = data[key];
			autoclickData.IsCritical += crit;
			autoclickData.Amount += (BigNumber)times;
			autoclickData.Shards += shards * times;
			if (exp.Mantissa > 0.0)
			{
				autoclickData.PetExp += GameManager.Instance.CurrentPet.ExpBonus.ApplyModOnVar(exp) * times;
			}
			tempAC += times;
		}
	}

	public string click_profit_str = "1";

	public string crit_chance_str = "5";

	public string crit_profit_str = ".5";

	[SerializeField]
	public GameObject VisualContainer;

	public GameObject OrbVisualContainer;

	public GameObject OrbContainer;

	public Collider2D Collider;

	public VariableComplex click_profit;

	public VariableComplex crit_chance;

	public VariableComplex crit_profit;

	public VariableFloat crit_additional_proc;

	public VariableComplex autoclick_profit;

	public VariableComplex percent_pps;

	public VariableComplex autoclicksFromSpell;

	public ParticleSystem[] Passive;

	public ParticleSystem CritEffect;

	public AnimatedShards AnimatedShards;

	public Action<float> OnClick;

	public Action<float> OnAutoClick;

	public Action OnManualClick;

	public Action OnMega;

	public Action<float> OnCrit;

	public Action<float> OnCritAuto;

	public MegaClick mega;

	public CritRatingCalculations critRating;

	public OrbVisual Defalut;

	public OrbVisual Custom;

	private BigNumber autoclicksChange = 0.0;

	private int manualClicksChange;

	private float clicksUpdateTimer;

	private float clicksUpdatePeriod = 0.2f;

	public VariableInt autoclicks;

	private EffectAutoClick autoclick_effect;

	public IncomeCounter ClickCounter;

	private AutoclickPool autoclickPool;

	public VariableFloat AutoclicksPerSecond;

	private Color orange = new Color(255f, 165f, 0f);

	private const float MaxEventChunk = 1.0737418E+09f;

	private const double CritRemainderEpsilon = 1E-06;

	private double crit_amount_float;

	public float GetCritChange
	{
		get
		{
			if (crit_chance.Value >= 100.0)
			{
				return 100f;
			}
			return crit_chance.ValueFloat;
		}
	}

	public IncomeCounter AutoclickCounter => autoclickPool.AutoclickCounter;

	public double getAutoclickPerSec
	{
		get
		{
			return autoclickPool.autoclickPerSec;
		}
		set
		{
			autoclickPool.autoclickPerSec = value;
		}
	}

	public void Init()
	{
		click_profit = new VariableComplex(click_profit_str);
		crit_chance = new VariableComplex(crit_chance_str);
		crit_profit = new VariableComplex(crit_profit_str);
		crit_additional_proc = new VariableFloat(0f);
		critRating = new CritRatingCalculations();
		autoclick_profit = new VariableComplex(1.0);
		percent_pps = new VariableComplex(0.0);
		autoclicksFromSpell = new VariableComplex(1.0);
		autoclicks = new VariableInt(0);
		autoclick_effect = new EffectAutoClick(0f, 1f, 1f, 1f, crit_const: false, 1f, fromSpell: false);
		autoclicksChange = (manualClicksChange = 0);
		AutoclicksPerSecond = new VariableFloat(0f);
		ClickCounter = new IncomeCounter(20);
		autoclickPool = new AutoclickPool();
	}

	public void Restart()
	{
		click_profit.Reset();
		crit_chance.Reset();
		crit_profit.Reset();
		click_profit.SetValue(click_profit_str);
		crit_chance.SetValue(crit_chance_str);
		crit_profit.SetValue(crit_profit_str);
		autoclick_profit.SetValue(1.0);
		percent_pps.SetValue(0.0);
		critRating.Restart();
		AnimatedShards.Restart();
		critRating.Recalculate();
		autoclickPool.Reset();
		autoclicksChange = (manualClicksChange = 0);
		crit_amount_float = 0.0;
	}

	public void SetOrbVisual(string prefabName)
	{
		if (prefabName == HeroesNames.Apprentice.ToString())
		{
			Defalut.gameObject.SetActive(value: true);
			if (Settings.OrbParticles)
			{
				Defalut.PlayParticles();
			}
			if (Custom != null)
			{
				Custom.gameObject.SetActive(value: false);
			}
		}
		else
		{
			switchOrb(prefabName);
		}
	}

	private void switchOrb(string key)
	{
		Defalut.gameObject.SetActive(value: false);
		if (Custom != null && Custom.name == key)
		{
			Custom.gameObject.SetActive(value: true);
			if (Settings.OrbParticles)
			{
				Custom.PlayParticles();
			}
			return;
		}
		if (Custom != null)
		{
			UnityEngine.Object.Destroy(Custom.gameObject);
		}
		OrbVisual orbVisual = Resources.Load<OrbVisual>("Orbs/" + key);
		Vector3 localScale = orbVisual.transform.localScale;
		Vector3 localPosition = orbVisual.transform.localPosition;
		Custom = UnityEngine.Object.Instantiate(orbVisual, OrbContainer.transform);
		Custom.name = key;
		Custom.transform.localPosition = localPosition;
		Custom.transform.localScale = localScale;
		Custom.SetPassive();
		if (Settings.OrbParticles)
		{
			Custom.PlayParticles();
		}
	}

	private void Update()
	{
		critRating.Update();
		if (clicksUpdateTimer > clicksUpdatePeriod)
		{
			if (autoclicksChange > 0.0)
			{
				Statistic.AutoClicks.Change(autoclicksChange);
				Statistic.Change(Statistic.AutoClicksTotal, autoclicksChange);
				Statistic.Change(Statistic.AutoClicksRealm, autoclicksChange);
				autoclicksChange = 0.0;
			}
			if (manualClicksChange > 0)
			{
				Statistic.Clicks.Change(manualClicksChange);
				Statistic.ClicksTotal.Change(manualClicksChange);
				Statistic.ClicksRealm.Change(manualClicksChange);
				manualClicksChange = 0;
			}
			clicksUpdateTimer = 0f;
		}
		if (Time.timeScale != 0f)
		{
			clicksUpdateTimer += Time.deltaTime / Time.timeScale;
		}
		if ((float)autoclicks.ValueInt != autoclick_effect.Frequency)
		{
			autoclick_effect.Frequency = autoclicks.ValueInt;
		}
		autoclick_effect.Update();
		autoclickPool.Update(Time.unscaledDeltaTime);
		if ((double)AutoclicksPerSecond.ValueFloat != getAutoclickPerSec)
		{
			AutoclicksPerSecond.SetValue(getAutoclickPerSec);
		}
	}

	public void debug()
	{
		Debug.Log(crit_chance.debug() + " " + crit_profit.debug());
	}

	public void Click()
	{
		if (mega == null || mega.megaCharges.Value < 1.0)
		{
			GameManager.Instance.Idle.OnClickOrb();
		}
		click();
	}

	public void UpdateSimulationSpeed()
	{
	}

	public void AutoClick(BigNumber profit, BigNumber crit_chance_arg, BigNumber crit_profit_arg, float shard_drop, bool pet_click = false, bool idle_break = false, float pet_exp = 1f, float times = 1f)
	{
		BigNumber bigNumber = profit * autoclick_profit.Value;
		float num = 0f;
		if (times == 1f)
		{
			num = checkCrit(crit_chance_arg);
			if (num >= 0f)
			{
				num = 1f;
				OnCrit?.Invoke(1f);
				OnCritAuto?.Invoke(1f);
				bigNumber *= 1.0 + crit_profit_arg;
			}
			ProcessAutoclick(bigNumber, 1.0, num > 0f, shard_drop, pet_click, idle_break, pet_exp);
			return;
		}
		float num2 = ((crit_chance_arg == 0.0) ? ((BigNumber)GetCritChange) : crit_chance_arg).ToFloat();
		double num3 = crit_amount_float + (double)(times * num2) / 100.0;
		double num4 = Math.Floor(num3);
		crit_amount_float = num3 - num4;
		if (crit_amount_float < 0.0 && Math.Abs(crit_amount_float) <= 1E-06)
		{
			crit_amount_float = 0.0;
		}
		else if (crit_amount_float > 1.0 && crit_amount_float - 1.0 <= 1E-06)
		{
			crit_amount_float = 0.0;
			num4 += 1.0;
		}
		double num5 = times;
		if (num4 > num5)
		{
			num4 = num5;
			crit_amount_float = 0.0;
		}
		if (num4 >= 1.0)
		{
			InvokeChunked(num4, OnCrit);
			InvokeChunked(num4, OnCritAuto);
			ProcessAutoclick(bigNumber * (1.0 + crit_profit_arg), num4, isCrit: true, shard_drop, pet_click, idle_break, pet_exp);
			num5 -= num4;
		}
		if (num5 > 0.0)
		{
			ProcessAutoclick(bigNumber, num5, isCrit: false, shard_drop, pet_click, idle_break, pet_exp);
		}
		if (crit_amount_float < 0.0)
		{
			Debug.LogError("crit_amount_float < 0");
		}
	}

	private void InvokeChunked(double amount, Action<float> action)
	{
		if (action != null && !(amount <= 0.0))
		{
			double num = amount;
			while (num > 0.0)
			{
				float num2 = ((num > 1073741824.0) ? 1.0737418E+09f : ((float)num));
				action(num2);
				num -= (double)num2;
			}
		}
	}

	private void ProcessAutoclick(BigNumber per_click, double times, bool isCrit, float shard_drop, bool pet_click = false, bool idle_break = false, float pet_exp = 1f)
	{
		if (idle_break && GameManager.Instance.Idle != null)
		{
			GameManager.Instance.Idle.Restart();
		}
		BigNumber shards = 0.0;
		if (shard_drop != 0f)
		{
			shards = GameManager.Instance.Scrolls.ShardsPerClick.Value * shard_drop * 0.20000000298023224;
		}
		BigNumber exp = 0.0;
		if (pet_click && GameManager.Instance.CurrentPet.Pet != null)
		{
			exp = pet_exp;
		}
		BigNumber crit = (isCrit ? new BigNumber(times) : ((BigNumber)0.0));
		autoclickPool.Add(per_click, shards, exp, crit, times);
		autoclicksChange += (BigNumber)times;
		InvokeChunked(times, OnClick);
		InvokeChunked(times, OnAutoClick);
	}

	private void click()
	{
		BigNumber bigNumber = click_profit.Value;
		int num = checkCrit(GetCritChange);
		if (num >= 0 && OnCrit != null)
		{
			OnCrit(1f);
		}
		if (mega != null && mega.megaCharges.Value > 0.0)
		{
			if (!mega.Super)
			{
				bigNumber *= mega.megaProfit.Value * mega.GetHC();
				if (num >= 0)
				{
					bigNumber *= 1.0 + crit_profit.Value;
				}
			}
			else
			{
				bigNumber = click_profit.Value * (1.0 + GetCritChange * crit_profit.Value / 100.0) * mega.megaProfit.Value * mega.megaCharges.Value.Pow(mega.superPower);
				num = 0;
			}
			text_up_mega(bigNumber, num);
			if (OnMega != null)
			{
				OnMega();
			}
			mega.onMega(mega.Super);
		}
		else
		{
			if (num >= 0)
			{
				bigNumber *= 1.0 + crit_profit.Value;
				if (Settings.Particles)
				{
					Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
					position.z = 0f;
					CritEffect.transform.position = position;
					CritEffect.Play();
				}
			}
			text_up(bigNumber, num, auto: false, 1.0);
		}
		GameManager.Instance.ManaChange(bigNumber);
		ClickCounter.Add(bigNumber);
		DropShards(GameManager.Instance.Scrolls.GetProgressFromClick());
		manualClicksChange++;
		if (OnClick != null)
		{
			OnClick(1f);
		}
		if (OnManualClick != null)
		{
			OnManualClick();
		}
	}

	private void DropShards(BigNumber dp)
	{
		if (Settings.ThrowShards && OrbContainer.activeInHierarchy)
		{
			AnimatedShards.ThrowShard(dp);
		}
		else
		{
			GameManager.Instance.Scrolls.ShardsPool.Add(dp);
		}
	}

	private void text_up_mega(BigNumber profit, int crit)
	{
		if (GameManager.Instance.AnimatedText.AvailableText)
		{
			Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			position.z = 0f;
			Color color = ((crit >= 0) ? orange : Color.yellow);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(profit.ToReadableString());
			if (VisualContainer.activeSelf)
			{
				GameManager.Instance.AnimatedText.TextUp(stringBuilder.ToString(), position, color, 1f, 1.25f + 0.05f * (float)crit, ignoreLimits: false, ignoreTimeScale: true);
			}
		}
	}

	private void text_up(BigNumber profit, BigNumber crit, bool auto, BigNumber amount)
	{
		if (GameManager.Instance.AnimatedText.AvailableText && base.gameObject.activeInHierarchy)
		{
			Vector3 position;
			if (auto)
			{
				position = base.transform.position;
			}
			else
			{
				Vector3 vector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				vector.z = 0f;
				position = vector;
			}
			Color white = Color.white;
			white.g = 1f - (crit / amount).ToFloat();
			white.b = 1f - (crit / amount).ToFloat();
			white.r = 1f;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(profit.ToReadableString());
			if (amount > 1.0)
			{
				stringBuilder.Append(" (<sprite=0 tint=1>");
				stringBuilder.Append(amount.ToReadableString("F0"));
				stringBuilder.Append(")");
			}
			if (VisualContainer.activeSelf)
			{
				float size = 1.25f;
				GameManager.Instance.AnimatedText.TextUp(stringBuilder.ToString(), position, white, 1f, size, ignoreLimits: false, ignoreTimeScale: true, 2f);
			}
		}
	}

	private int checkCrit(BigNumber chance)
	{
		int result = -1;
		if (chance < 0.0)
		{
			return result;
		}
		double num = ((chance == 0.0) ? ((BigNumber)GetCritChange) : chance).ToDouble();
		if ((double)UnityEngine.Random.Range(0f, 100f) <= num)
		{
			result = 0;
		}
		return result;
	}
}
