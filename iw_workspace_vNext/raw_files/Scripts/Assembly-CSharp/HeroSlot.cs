using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroSlot : MonoBehaviour
{
	public Hero Hero;

	public HeroesNames DefaultClass;

	public VariableBignumber HeroExp;

	public VariableBignumber ExpBoost;

	public VariableBignumber ExpMult;

	public VariableComplex ExpManaSources;

	public VariableBignumber ExpFlat;

	public VariableBignumber ExpStack;

	public VariableBignumber ClassBonusStacks;

	public VariableBignumber APSpeed;

	public TextMeshProUGUI LevelLabel;

	public TextMeshProUGUI AscensionLevelLabel;

	public RectTransform XPbarRect;

	public Image ExpBar;

	public TextMeshProUGUI Tip;

	public PortraitFrame Portrait;

	public HeroChoosePanel HeroPanel;

	public VariableInt Level;

	public VariableInt AddLevel;

	public VariableLong PlayedTime;

	public VariableBignumber SkipedPlayedTime;

	public VariableInt StartingLevel;

	public VariableComplex AbilityPower;

	public TextMeshProUGUI Quote;

	public Image QuoteImage;

	public BarkColors QuoteColors;

	public ParticleSystem LevelUpEffect;

	public TextMeshProUGUI DistortionTime;

	public ItemSetIconFlash IconSetAnim;

	[SerializeField]
	private BottomPanelButton sign;

	private Timer timer;

	public bool tip_active;

	public float GrowBaseLevel = 1.09f;

	public Action OnPreSelect;

	public Action OnSelect;

	public Action OnChange;

	private float updateTimer;

	private float checkUnlocksTimer = 10f;

	public Action OnMirror;

	public void Init()
	{
		HeroExp = new VariableBignumber(0.0);
		ExpBoost = new VariableBignumber(1.0);
		ExpMult = new VariableBignumber(1.0);
		ExpManaSources = new VariableComplex(1.0);
		ExpFlat = new VariableBignumber(0.0);
		ExpStack = new VariableBignumber(1.0);
		Level = new VariableInt(1);
		PlayedTime = new VariableLong(1uL);
		SkipedPlayedTime = new VariableBignumber(0.0);
		StartingLevel = new VariableInt(1);
		AbilityPower = new VariableComplex(1.0);
		ClassBonusStacks = new VariableBignumber(0.0);
		APSpeed = new VariableBignumber(1.0);
		AddLevel = new VariableInt(0);
		DefaultClass = HeroesNames.Apprentice;
		HeroesChoose heroesChoose = HeroPanel.Heroes[0];
		heroesChoose.Init();
		ActivateHero(heroesChoose.Hero);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.Level, Level);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.AddLevel, AddLevel);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.Exp, HeroExp);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.StartingLevel, StartingLevel);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.Time, PlayedTime);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.MaxLevelAllTime, Statistic.HeroMaxLevelAllTime);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.ApprenticeMaxLevelRealm, Statistic.ApprenticeMaxLevelRealm);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.AbilityPower, AbilityPower);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.StackBonus, ClassBonusStacks);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.ExpBoost, ExpBoost);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.ExpMult, ExpMult);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.ExpMS, ExpManaSources);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.APSpeed, APSpeed);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.HC, Statistic.HCTotal);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.LS, Statistic.LSTotal);
		GameContext.ContextAddResource(ResourceType.Hero.ToString() + "." + ResourceHero.CT, Statistic.CTTotal);
		AscensionManager ascension = GameManager.Instance.Ascension;
		ascension.OnAscension = (Action)Delegate.Combine(ascension.OnAscension, new Action(onAscension));
	}

	public void OnLevelUp()
	{
		if (Settings.Particles && !LevelUpEffect.isEmitting)
		{
			LevelUpEffect.Play();
		}
	}

	public void Restart()
	{
		StopAllCoroutines();
		ClassBonusStacks.SetValue(0.0);
		HeroExp.SetValue(0.0);
		ExpFlat.SetValue(0.0);
		ExpStack.SetValue(1.0);
		Level.SetValue(StartingLevel.ValueInt);
		PlayedTime.SetValue(1uL);
		SkipedPlayedTime.SetValue(0.0);
		AbilityPower.Reset(1.0);
		Hero.UpdateExp();
		SetHero(DefaultClass);
		ExpBar.fillAmount = 0f;
		LevelLabel.text = "";
		HeroPanel.Restart();
	}

	public void PostLoad()
	{
		foreach (HeroesChoose hero in HeroPanel.Heroes)
		{
			hero.Check();
		}
		sign.TurnOff();
		Hero.PostLoad();
	}

	private void Update()
	{
		Hero.update();
		updateTimer += Time.unscaledDeltaTime;
		if (updateTimer > 0.25f)
		{
			UpdateExpLabel();
			UpdateDistortionTime();
			updateTimer = 0f;
		}
		checkUnlocksTimer += Time.unscaledDeltaTime;
		if (!(checkUnlocksTimer > 30f))
		{
			return;
		}
		foreach (HeroesChoose hero in HeroPanel.Heroes)
		{
			if (!hero.Hero.Unlocked && hero.CheckAvailable())
			{
				break;
			}
		}
		checkUnlocksTimer = 0f;
	}

	private void LateUpdate()
	{
		Hero.UpdateExp();
	}

	private void UpdateExpLabel()
	{
		LevelLabel.text = Hero.Level.ValueInt.ToString();
		if (!GameManager.Instance.Ascension.IsActive)
		{
			ExpBar.fillAmount = Hero.LevelProgress;
			return;
		}
		DemonForm current = GameManager.Instance.Ascension.GetCurrent();
		ExpBar.fillAmount = current.LevelProgress;
		AscensionLevelLabel.text = current.Level.ValueInt.ToString();
	}

	private void onAscension()
	{
		AscensionLevelLabel.transform.parent.gameObject.SetActive(GameManager.Instance.Ascension.IsActive);
		if (GameManager.Instance.Ascension.IsActive)
		{
			XPbarRect.sizeDelta = new Vector2(61f, XPbarRect.sizeDelta.y);
		}
		else
		{
			XPbarRect.sizeDelta = new Vector2(86f, XPbarRect.sizeDelta.y);
		}
	}

	private void UpdateDistortionTime()
	{
		if (Time.timeScale > 1f)
		{
			if (!DistortionTime.gameObject.activeInHierarchy)
			{
				DistortionTime.gameObject.SetActive(value: true);
			}
			DistortionTime.text = "Time distortion x" + Time.timeScale.ToString("F2", CultureInfo.InvariantCulture);
		}
		else if (DistortionTime.gameObject.activeInHierarchy)
		{
			DistortionTime.gameObject.SetActive(value: false);
		}
	}

	public void ShowTip()
	{
		Tip.transform.parent.position = base.transform.position + new Vector3(-0.5f, -0.8f, 0f);
		Tip.transform.parent.gameObject.SetActive(value: true);
		timer = GameManager.Instance.Timers.GetPeriodical(180f, 0.5f, delegate
		{
			Tip.text = GetTip();
		}, HideTip, ignore_scale: true);
		Tip.text = GetTip();
		tip_active = true;
	}

	private string GetTip()
	{
		if (!GameManager.Instance.Ascension.IsActive)
		{
			return Hero.Tips_text();
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(GameManager.Instance.Ascension.GetCurrent().TipText());
		stringBuilder.AppendLine();
		stringBuilder.Append(Hero.Tips_text());
		return stringBuilder.ToString();
	}

	public void HideTip()
	{
		timer.Stop();
		Tip.text = "";
		Tip.transform.parent.gameObject.SetActive(value: false);
		tip_active = false;
	}

	public void ActivateHero(Hero hero, bool inReset = false)
	{
		if (Hero != hero)
		{
			if (OnPreSelect != null)
			{
				OnPreSelect();
			}
			if (Hero != null)
			{
				Hero.DisableAll();
			}
			PlayedTime.SetValue(1uL);
			GameManager.Instance.Idle.Restart();
			Hero = hero;
			Portrait.UpdateFrame();
			Hero.Level = Level;
			if (GameManager.Instance.ChallengeManager.StatsIsOn())
			{
				Hero.Level.Change(0);
			}
			Hero.experience = HeroExp;
			GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(0.0);
			GameManager.Instance.VoidMana.SetValue(0.0);
			Hero.ApplyEffects();
			GameManager.Instance.Interior.Orbs.ActivateCurrent();
			if (OnSelect != null)
			{
				OnSelect();
			}
			ChangeColorQuote();
			if (Settings.Cursor)
			{
				GameManager.Instance.Interior.Cursors.UpdateCursor();
			}
		}
	}

	public void SetHero(HeroesNames key)
	{
		HeroesChoose hero = HeroPanel.GetHero(key);
		SetHero(hero);
	}

	public void SetHero(HeroesChoose h)
	{
		ActivateHero(h.Hero);
		GameManager.Instance.SpellBook.ChangeSpellSet();
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public void Mirror()
	{
		Settings.MirrorChar = !Settings.MirrorChar;
		if (OnMirror != null)
		{
			OnMirror();
		}
	}

	public void AddFlatExp(BigNumber value)
	{
		ExpFlat.Change(value);
	}

	public void AddExpStack(float value)
	{
		ExpStack.Change(0.0, value);
	}

	private void ChangeColorQuote()
	{
		QuoteImage.color = QuoteColors.Get(Hero.NameKey);
	}
}
