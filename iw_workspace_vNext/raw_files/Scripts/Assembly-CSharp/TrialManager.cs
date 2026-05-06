using System;
using System.Collections.Generic;
using System.Text;
using ModelShark;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrialManager : MonoBehaviour, ITooltipBodyHandler
{
	public VariableInt Keys;

	public VariableInt Completed;

	public VariableBignumber CompletedValue;

	public VariableFloat CompletedBonus;

	public VariableInt CompletedStartedBonus;

	public VariableInt TotalCompleted;

	public VariableComplex TimeReduction;

	public VariableFloat TimeFlatReduction;

	public VariableComplex RewardSize;

	public VariableComplex AdditionalRewardSize;

	public VariableFloat AdditionalRewardCrit;

	public VariableInt maxRunes;

	public VariableInt apTimeReduction;

	public VariableFloat ResearchSpeed;

	public VariableInt Milestones;

	public VariableInt MilestonesCap;

	public TrialRewardList RewardList;

	public QuestManager QuestManager;

	public List<TrialChoose> chooses;

	public List<Trial> trials;

	public int tries;

	public TrialConfirmation TrialConfirm;

	public TrialQuestConfirmation QuestConfirm;

	public Action<Trial> OnActivation;

	public Action<Trial> OnComplete;

	public Action OnStart;

	[SerializeField]
	private TextMeshProUGUI KeyLabel;

	[SerializeField]
	private TextMeshProUGUI TimerLabel;

	[SerializeField]
	private GameObject NextIn;

	[SerializeField]
	private Image TimerProgress;

	[SerializeField]
	private TextMeshProUGUI CompletedLabel;

	[SerializeField]
	private TextMeshProUGUI PassiveBonusLabel;

	[SerializeField]
	private TextMeshProUGUI next_milestone;

	[SerializeField]
	private TextMeshProUGUI progress_milestone;

	[SerializeField]
	private PromoShowResult showResult;

	[SerializeField]
	private TrialMilestonesListSwitch milestoneSwitch;

	[SerializeField]
	private Transform milestoneParent;

	[SerializeField]
	private FlexTip tip;

	[SerializeField]
	private ModelShark.TooltipTrigger tooltip;

	[SerializeField]
	private BottomPanelButton sign;

	public Button start;

	public Action<float> OnTrialSkip;

	public Func<PromoShowResult.Data, PromoShowResult.Data> OnGetReward;

	private List<TextMeshProUGUI> milestones;

	private TrialRewards rewards;

	private TrialBonuses bonuses;

	private List<DustGambling.SpriteMap> sprites;

	private Trial selected;

	private float nextRuneIn;

	private float maxTime => 86400f - 3600f * TimeFlatReduction.ValueFloat;

	private void OnEnable()
	{
		if (selected != null)
		{
			UpdateRewardDescription(selected);
		}
		else
		{
			RewardList.gameObject.SetActive(value: false);
		}
		chooses.Find((TrialChoose x) => x.ID == Trials.Skill).button.interactable = !GameManager.Instance.Realmcraft.IsActive;
	}

	private void Update()
	{
		start.interactable = selected != null && Keys.ValueInt > 0 && !selected.IsActive;
		KeyLabel.text = Keys.ValueInt.ToString();
		if (Keys.ValueInt >= maxRunes.ValueInt)
		{
			NextIn.SetActive(value: false);
			return;
		}
		NextIn.SetActive(value: true);
		float num = nextRuneIn / TimeReduction.ValueFloat;
		TimerLabel.text = Statistic.time_to_string((int)num, full: true);
		float fillAmount = 1f - nextRuneIn / maxTime;
		TimerProgress.fillAmount = fillAmount;
	}

	private void update_completed()
	{
		CompletedValue.SetValue(GameManager.Instance.Trials.GetCompleted());
	}

	private void update_milestone()
	{
		Milestones.SetValue(Mathf.Clamp(CompletedValue.Value.ToInt(), 0, MilestonesCap.ValueInt));
	}

	public void InitContext()
	{
		Keys = new VariableInt(1);
		Completed = new VariableInt(0);
		CompletedBonus = new VariableFloat(1f);
		CompletedStartedBonus = new VariableInt(0);
		CompletedValue = new VariableBignumber(0.0);
		TotalCompleted = new VariableInt(0);
		Milestones = new VariableInt(0);
		MilestonesCap = new VariableInt(400);
		RewardSize = new VariableComplex(1.0);
		TimeReduction = new VariableComplex(1.0);
		TimeFlatReduction = new VariableFloat(0f);
		AdditionalRewardSize = new VariableComplex(1.0);
		AdditionalRewardCrit = new VariableFloat(0f);
		apTimeReduction = new VariableInt(0);
		maxRunes = new VariableInt(10);
		ResearchSpeed = new VariableFloat(1f);
		AddToContext();
	}

	public void Init()
	{
		trials = new List<Trial>
		{
			new TrialChallenge(Trials.Skill),
			new TrialCrystalBall(Trials.Valor),
			new TrialResearch(Trials.Innovation),
			new TrialLong(Trials.Patience),
			new TrialQuest(Trials.Talent)
		};
		sprites = GameManager.Instance.Craft.window.craftingMenu.sprites;
		rewards = new TrialRewards(sprites, showResult);
		rewards.Init();
		bonuses = new TrialBonuses();
		bonuses.Init();
		milestoneSwitch.Init(bonuses);
		QuestManager = new QuestManager();
		QuestManager.Init();
		VariableLong timeSession = Statistic.TimeSession;
		timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(Tick));
		tooltip.bodyHandler = this;
		VariableInt keys = Keys;
		keys.OnChange = (Action)Delegate.Combine(keys.OnChange, new Action(CheckSign));
		VariableInt variableInt = maxRunes;
		variableInt.OnChange = (Action)Delegate.Combine(variableInt.OnChange, new Action(CheckSign));
		VariableBignumber completedValue = CompletedValue;
		completedValue.OnChange = (Action)Delegate.Combine(completedValue.OnChange, new Action(update_milestone));
		VariableInt completed = Completed;
		completed.OnChange = (Action)Delegate.Combine(completed.OnChange, new Action(update_completed));
		VariableFloat completedBonus = CompletedBonus;
		completedBonus.OnChange = (Action)Delegate.Combine(completedBonus.OnChange, new Action(update_completed));
		VariableInt completedStartedBonus = CompletedStartedBonus;
		completedStartedBonus.OnChange = (Action)Delegate.Combine(completedStartedBonus.OnChange, new Action(update_completed));
		VariableInt milestonesCap = MilestonesCap;
		milestonesCap.OnChange = (Action)Delegate.Combine(milestonesCap.OnChange, new Action(update_completed));
	}

	public void PreExile()
	{
		bonuses.UnSub();
	}

	public void PostExile()
	{
		bonuses.Sub();
		bonuses.UpdateList();
	}

	public void RealmReset()
	{
		Completed.SetValue(0);
	}

	public void PostRealmReset()
	{
		bonuses.RealmReset();
	}

	private void AddToContext()
	{
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.Key, Keys);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.Reward, RewardSize);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.ARew, AdditionalRewardSize);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.KeyCap, maxRunes);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.TimeReduction, TimeReduction);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.TimeFlatReduction, TimeFlatReduction);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.Completed, Completed);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.CompletedBonus, CompletedBonus);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.CompleteStart, CompletedStartedBonus);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.CompletedValue, CompletedValue);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.TotalCompleted, TotalCompleted);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.APReduction, apTimeReduction);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.ARCrit, AdditionalRewardCrit);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.Milestones, Milestones);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.MilestonesCap, MilestonesCap);
		GameContext.ContextAddResource(ResourceType.Trial.ToString() + "." + TrialKeys.InnovationSpeed, ResearchSpeed);
	}

	public int GetCompleted()
	{
		return Mathf.FloorToInt((float)(Completed.ValueInt + CompletedStartedBonus.ValueInt) * CompletedBonus.ValueFloat);
	}

	public void HardReset()
	{
		ResetTrial();
		Keys.SetValue(0);
		Completed.SetValue(0);
	}

	public void PreLoad()
	{
		bonuses.Preload();
		ResetTrial();
	}

	public void PostLoad()
	{
		Trial trial = GetTrial(Trials.Skill);
		if (trial.IsActive)
		{
			trial.Load();
		}
		bonuses.Postlaod();
	}

	public void Open()
	{
		RewardList.Recheck();
		NextIn.SetActive(Keys.ValueInt < maxRunes.ValueInt);
		UpdateLabelsOpen();
		UpdateListText();
		UpdateNextMilestone();
		if (selected != null)
		{
			UpdateRewardDescription(selected);
		}
		else
		{
			RewardList.gameObject.SetActive(value: false);
		}
		base.gameObject.SetActive(value: true);
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}

	public void Select(Trials id)
	{
		selected = GetTrial(id);
		foreach (TrialChoose choose in chooses)
		{
			choose.SetHightlight(choose.ID == id);
		}
		UpdateRewardDescription(selected);
	}

	private void UpdateRewardDescription(Trial current)
	{
		TrialChoose choose = GetChoose((Trials)current.ID);
		RewardList.SetReward(RewardSize.ValueFloat * current.RewardSize, choose);
		RewardList.gameObject.SetActive(value: true);
	}

	public void StartTrial()
	{
		if (Keys.ValueInt > 0 && selected != null)
		{
			selected.Start();
			if (nextRuneIn == 0f)
			{
				nextRuneIn = maxTime;
			}
		}
	}

	public void CompleteTrial(int id)
	{
		Completed.Change(1);
		TotalCompleted.Change(1);
		bonuses.UpdateList();
		OnTrialEnd();
		if (apTimeReduction.ValueInt > 0)
		{
			BigNumber bigNumber = GameManager.Instance.AttributeManager.need * 0.01 * apTimeReduction.ValueInt;
			if (bigNumber > 7200.0)
			{
				bigNumber = 7200.0 + (bigNumber - 7200.0).Pow(0.5);
			}
			GameManager.Instance.AttributeManager.Offline(bigNumber);
		}
		Trial trial = GetTrial((Trials)id);
		bool isCrit = false;
		if (GameManager.Instance.Trials.AdditionalRewardCrit.ValueFloat > 1f)
		{
			isCrit = RandomSeed.Get(GetCritSeed(), 0f, 100f) < GameManager.Instance.Trials.AdditionalRewardCrit.ValueFloat;
		}
		rewards.GetReward(trial, isCrit);
		UpdateNextMilestone();
		UpdateCompletedLabel();
		if (OnComplete != null)
		{
			OnComplete(trial);
		}
		TrialChoose choose = GetChoose((Trials)id);
		if (choose.IsAuto)
		{
			choose.Launch();
		}
	}

	private int GetCritSeed()
	{
		return Completed.ValueInt + 1357;
	}

	public void ExitTrial()
	{
		OnTrialEnd();
	}

	private void OnTrialEnd()
	{
		tries++;
		if (Keys.ValueInt <= maxRunes.ValueInt && nextRuneIn == 0f)
		{
			nextRuneIn = maxTime;
		}
	}

	public void ResetTrial()
	{
		foreach (Trial trial in trials)
		{
			if (trial.IsActive)
			{
				trial.End();
			}
		}
	}

	public void PreOffline()
	{
		TrialResearch trialResearch = GetTrial(Trials.Innovation) as TrialResearch;
		TrialLong obj = GetTrial(Trials.Patience) as TrialLong;
		trialResearch.UnsubscribeTime();
		obj.UnsubscribeTime();
	}

	public int Offline(int sec)
	{
		TrialResearch trialResearch = GetTrial(Trials.Innovation) as TrialResearch;
		TrialLong obj = GetTrial(Trials.Patience) as TrialLong;
		trialResearch.SubscribeTime();
		obj.SubscribeTime();
		if (!GameManager.Instance.Paragon.TrialsIsAvailable)
		{
			return 0;
		}
		int valueInt = Keys.ValueInt;
		Tick(sec);
		valueInt = Keys.ValueInt - valueInt;
		SkipTime(Mathf.Min(sec, 604800));
		return valueInt;
	}

	public void SkipTime(float sec)
	{
		TrialResearch trialResearch = GetTrial(Trials.Innovation) as TrialResearch;
		TrialLong trialLong = GetTrial(Trials.Patience) as TrialLong;
		trialResearch.SubscribeTime();
		trialLong.SubscribeTime();
		SkipAuto(trialLong, trialResearch, sec);
		if (OnTrialSkip != null)
		{
			OnTrialSkip(sec);
		}
	}

	private void SkipAuto(TrialLong tl, TrialResearch tr, float sec)
	{
		if ((!tr.IsActive && !tl.IsActive) || sec <= 0f)
		{
			return;
		}
		float num = sec;
		if (tl.IsActive)
		{
			num = tl.GetLeft();
			if (num > sec)
			{
				num = sec;
			}
			tl.update(num);
		}
		float num2 = num;
		while (num2 > 0f)
		{
			if (tr.IsActive)
			{
				float num3 = tr.GetLeft();
				if (num3 > num2)
				{
					num3 = num2;
				}
				tr.update(num3);
				num2 -= num3;
			}
			else
			{
				num2 = 0f;
			}
		}
		SkipAuto(tl, tr, sec - num);
	}

	public void HideTip()
	{
		tip.Close();
	}

	private void Tick()
	{
		Tick(1);
	}

	private void Tick(int sec)
	{
		if (Time.timeScale == 0f)
		{
			return;
		}
		if (Keys.ValueInt >= maxRunes.ValueInt)
		{
			nextRuneIn = 0f;
			return;
		}
		if (nextRuneIn <= 0f)
		{
			nextRuneIn = maxTime;
		}
		nextRuneIn -= (float)sec * TimeReduction.ValueFloat;
		while (nextRuneIn <= 0f && Keys.ValueInt < maxRunes.ValueInt)
		{
			Keys.Change(1);
			if (Keys.ValueInt < maxRunes.ValueInt)
			{
				nextRuneIn += maxTime;
			}
			else
			{
				nextRuneIn = 0f;
			}
		}
	}

	private void UpdateCompletedLabel()
	{
		if (CompletedBonus.ValueFloat == 1f && CompletedStartedBonus.ValueInt == 0)
		{
			CompletedLabel.text = GetCompleted().ToString();
		}
		else
		{
			CompletedLabel.text = GetCompleted() + " (" + Completed.ValueInt + ")";
		}
	}

	private void UpdateLabelsOpen()
	{
		UpdateCompletedLabel();
		string text = bonuses.Passive.Preview("");
		if (Settings.ColoredTips)
		{
			text = "<color=#e2b018>" + text + "</color>";
		}
		string text2 = (bonuses.bonusPower.Value * 100.0).ToReadableString("F0") + "%";
		if (Settings.ColoredTips)
		{
			text2 = "<color=#e2b018>" + text2 + "</color>";
		}
		PassiveBonusLabel.text = "MystM".Translate() + " +" + text + "\n" + string.Format("TrialPassiveBonus".Translate(), text2);
	}

	private void UpdateListText()
	{
		bonuses.UpdateList();
		milestoneSwitch.UpdateListText();
	}

	private void UpdateNextMilestone()
	{
		Objective next = bonuses.GetNext();
		if (next == null)
		{
			next_milestone.text = "-";
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("[");
		stringBuilder.Append(next.Requirement.argument.ToInt().ToString());
		stringBuilder.Append("] ");
		stringBuilder.Append(next.Preview());
		next_milestone.text = stringBuilder.ToString();
	}

	public Trial GetTrial(Trials id)
	{
		int key = (int)id;
		return trials.Find((Trial x) => x.ID == key);
	}

	public TrialChoose GetChoose(Trials id)
	{
		return chooses.Find((TrialChoose x) => x.ID == id);
	}

	public TrialSave Save()
	{
		TrialSave trialSave = new TrialSave();
		trialSave.completed = Completed.ValueInt;
		trialSave.totalCompleted = TotalCompleted.ValueInt;
		trialSave.tries = tries;
		trialSave.timer = nextRuneIn;
		trialSave.keys = Keys.ValueInt;
		trialSave.Add(DustGambling.DropType.Relic, rewards.relicDrop.tries, rewards.relicDrop.good);
		trialSave.Add(DustGambling.DropType.Coin, rewards.coinsDrop.tries, rewards.coinsDrop.good);
		if ((GetTrial(Trials.Skill) as TrialChallenge).IsActive)
		{
			trialSave.tos = true;
		}
		TrialCrystalBall trialCrystalBall = GetTrial(Trials.Valor) as TrialCrystalBall;
		if (trialCrystalBall.IsActive)
		{
			trialSave.tov = new TrialSave.ValorSave();
			trialSave.tov.HP = trialCrystalBall.ball.HP;
			trialSave.tov.Uses = new List<int>();
			foreach (CrystalBallSkill skill in trialCrystalBall.ball.skills)
			{
				trialSave.tov.Uses.Add(skill.Uses);
			}
		}
		TrialResearch trialResearch = GetTrial(Trials.Innovation) as TrialResearch;
		TrialChoose choose = GetChoose(Trials.Innovation);
		if (trialResearch.IsActive)
		{
			trialSave.tor = new TrialSave.ResearchSave(trialResearch.GetProgress(), choose.IsAuto);
		}
		TrialLong trialLong = GetTrial(Trials.Patience) as TrialLong;
		choose = GetChoose(Trials.Patience);
		if (trialLong.IsActive)
		{
			trialSave.tol = new TrialSave.ResearchSave(trialLong.GetProgress(), choose.IsAuto);
		}
		TrialQuest trialQuest = GetTrial(Trials.Talent) as TrialQuest;
		if (trialQuest.IsActive)
		{
			trialSave.tq = new TrialSave.QuestSave(trialQuest.selected.ID, trialQuest.selected.GetProgress(), trialQuest.selected.GetObjective());
		}
		return trialSave;
	}

	public void Load(TrialSave save)
	{
		if (save == null)
		{
			return;
		}
		Completed.SetValue(save.completed);
		TotalCompleted.SetValue(save.totalCompleted);
		bonuses.UpdateList();
		tries = save.tries;
		nextRuneIn = save.timer;
		Keys.SetValue(save.keys);
		rewards.relicDrop.Load(save.Get(DustGambling.DropType.Relic));
		rewards.coinsDrop.Load(save.Get(DustGambling.DropType.Coin));
		if (save.tos)
		{
			(GetTrial(Trials.Skill) as TrialChallenge).PreLoad();
		}
		if (save.tov != null)
		{
			TrialCrystalBall trialCrystalBall = GetTrial(Trials.Valor) as TrialCrystalBall;
			trialCrystalBall.Init();
			trialCrystalBall.Load();
			trialCrystalBall.ball.HP = save.tov.HP;
			for (int i = 0; i < trialCrystalBall.ball.skills.Count; i++)
			{
				trialCrystalBall.ball.skills[i].Load(save.tov.Uses[i]);
			}
			trialCrystalBall.ball.RecheckRestart();
		}
		if (save.tor != null)
		{
			TrialResearch obj = GetTrial(Trials.Innovation) as TrialResearch;
			obj.Load();
			obj.SetProgress(save.tor.Progress);
			GetChoose(Trials.Innovation).IsAuto = save.tor.Auto;
		}
		if (save.tol != null)
		{
			TrialLong obj2 = GetTrial(Trials.Patience) as TrialLong;
			obj2.Load();
			obj2.SetProgress(save.tol.Progress);
			GetChoose(Trials.Patience).IsAuto = save.tol.Auto;
		}
		if (save.tq != null)
		{
			(GetTrial(Trials.Talent) as TrialQuest).Load(save.tq.id, save.tq.progress, save.tq.objective);
		}
	}

	public string GetBody()
	{
		return string.Format("TrialRuneTooltip".Translate(), maxRunes.ValueInt);
	}

	private void CheckSign()
	{
		if (GameManager.Instance.Paragon.TrialsIsAvailable && maxRunes.ValueInt - Keys.ValueInt <= 1)
		{
			sign.TurnOnRed();
		}
		else
		{
			sign.TurnOff();
		}
	}
}
