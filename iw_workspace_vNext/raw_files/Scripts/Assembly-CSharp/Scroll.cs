using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Scroll : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public string Keycode;

	public KeyCode Keycode1;

	public Spell spell;

	public Button button;

	public Image Frame;

	public Image Icon;

	public Image CDIcon;

	public Image ProgressBar;

	public TextMeshProUGUI CountLabel;

	public TextMeshProUGUI Tip;

	public Transform text_place;

	public Text text_prefab;

	public Button ChangeButton;

	public GameObject Fade;

	public bool AutoCast;

	public Sprite shards_progress;

	public Sprite charging_progress;

	private bool tip_active;

	private float timer;

	private IEnumerator cur_action;

	public bool active;

	public bool careful_autocast;

	public bool isNotChangable;

	private float castTimer;

	private float counterUpdateTimer;

	public float TimeOfAction;

	public float GetCastRate
	{
		get
		{
			if (spell == null)
			{
				return GameManager.Instance.Scrolls.CastRate.ValueFloat;
			}
			return 1f / ((GameManager.Instance.Scrolls.CastRate.ValueFloat + (float)spell.GetAdditionalCastRate()) * GameManager.Instance.Scrolls.CastRateMult.ValueFloat);
		}
	}

	public void Restart()
	{
		Clear();
		active = false;
		DeactiveChange();
		ChangeButton.interactable = true;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			Action();
		}
		else if (eventData.button == PointerEventData.InputButton.Right)
		{
			Change();
		}
	}

	public void SetSpell(Spell sp, bool onLoad = false)
	{
		if (!isNotChangable)
		{
			spell = sp;
			spell.SetEfficiency();
			castTimer = 0f;
			if (!onLoad)
			{
				spell.ResetCounters();
			}
		}
	}

	public void UpdateScroll()
	{
		if (!Settings.BlockInput && (Input.GetButtonDown(Keycode) || Input.GetKeyDown(Keycode1)) && !Input.GetKey(KeyCode.Z) && !Input.GetKey(KeyCode.Y) && !Input.GetKey(KeyCode.P) && !Input.GetKey(KeyCode.Q))
		{
			Action();
		}
		if (spell == null)
		{
			UpdateVisual();
			return;
		}
		spell.CheckProgress();
		UpdateTooltip();
		UpdateAutocast();
		UpdateVisual();
		if (spell != null && !AutoCast && !spell.active)
		{
			counterUpdateTimer += Time.unscaledDeltaTime;
			if (counterUpdateTimer >= 10f)
			{
				counterUpdateTimer = 0f;
				spell.ResetCounters();
			}
		}
	}

	public void UpdateTooltip()
	{
		if (tip_active)
		{
			if ((double)timer > 0.5)
			{
				update_tip();
				timer = 0f;
			}
			timer += Time.unscaledDeltaTime;
		}
	}

	public void UpdateAutocast()
	{
		if (spell == null)
		{
			return;
		}
		if (AutoCast)
		{
			if (careful_autocast)
			{
				if (spell.IsMax)
				{
					spell_active();
				}
			}
			else if (spell.chargeCount != 0)
			{
				spell_active();
			}
			else
			{
				castTimer = 0f;
			}
		}
		if (spell != null)
		{
			Fade.SetActive(!spell.AvailableToCast);
			castTimer += Time.unscaledDeltaTime;
		}
	}

	public int GetAutoMode()
	{
		if (!AutoCast)
		{
			return 0;
		}
		if (careful_autocast)
		{
			return 1;
		}
		return 2;
	}

	public void SetAutoMode(int mode, bool isLoading = false)
	{
		if (!AutoCast && mode > 0)
		{
			AutoCast = mode > 0;
			if (AutoCast)
			{
				GameManager.Instance.Scrolls.AddToAutoCast(this);
			}
		}
		careful_autocast = mode == 1;
		castTimer = 0f;
		if (!isLoading)
		{
			spell.ResetCounters();
		}
	}

	public void UpdateVisual()
	{
		if (AutoCast)
		{
			if (careful_autocast)
			{
				Frame.color = GameManager.Instance.Scrolls.CarefulAutocastColor;
			}
			else
			{
				Frame.color = GameManager.Instance.Scrolls.CarelessAutocastColor;
			}
		}
		else
		{
			Frame.color = Color.white;
		}
		ProgressBar.fillAmount = ((spell == null) ? 0f : Mathf.Lerp(ProgressBar.fillAmount, (float)(spell.GetProgressBuild / spell.FullBuild), 30f * Time.unscaledDeltaTime));
		CountLabel.text = ((spell == null) ? " " : new BigNumber(spell.chargeCount).ToReadableString("F0"));
	}

	public void Change()
	{
		if (!isNotChangable)
		{
			if (spell == null)
			{
				GameManager.Instance.Scrolls.ChoosePanel.Open(this);
			}
			else if (GameManager.Instance.ChallengeManager.ActiveChallenge == null || (GameManager.Instance.ChallengeManager.ActiveChallenge != null && !active))
			{
				GameManager.Instance.Scrolls.ChoosePanel.Open(this);
			}
		}
	}

	public void CheckInFilled()
	{
		List<Scroll> unfilled_scrolls = GameManager.Instance.Scrolls.unfilled_scrolls;
		if (spell == null)
		{
			if (unfilled_scrolls.Contains(this))
			{
				unfilled_scrolls.Remove(this);
			}
		}
		else
		{
			if (!spell.ShardsBuilding)
			{
				return;
			}
			if (spell.IsMax)
			{
				if (unfilled_scrolls.Contains(this))
				{
					unfilled_scrolls.Remove(this);
				}
			}
			else if (!unfilled_scrolls.Contains(this))
			{
				unfilled_scrolls.Add(this);
			}
		}
	}

	public void Clear(bool soft = false)
	{
		if (isNotChangable)
		{
			return;
		}
		HideTip();
		if (spell == null)
		{
			return;
		}
		if (spell != null)
		{
			spell.RemovePassive();
		}
		if (soft && GameManager.Instance.ChallengeManager.ActiveChallenge != null && active)
		{
			return;
		}
		StopAction();
		if (spell != null)
		{
			spell.choice = false;
			if (spell.ShardsBuilding && GameManager.Instance.Scrolls.unfilled_scrolls.Contains(this))
			{
				GameManager.Instance.Scrolls.unfilled_scrolls.Remove(this);
			}
			RemoveAutocast();
			if (spell.OnClear != null)
			{
				spell.OnClear();
			}
			if (GameManager.Instance.ChallengeManager.ActiveChallenge != null)
			{
				ChangeButton.interactable = true;
			}
			AutoCast = false;
		}
		CDIcon.fillAmount = 0f;
		CDIcon.enabled = false;
		Icon.color = new Color(1f, 1f, 1f, 0f);
		ProgressBar.fillAmount = 0f;
		ProgressBar.color = new Color(1f, 1f, 1f, 0f);
		CountLabel.text = "";
		ChangeButton.gameObject.SetActive(value: false);
		Fade.SetActive(value: true);
		StopAllCoroutines();
		cur_action = null;
		spell.ResetCounters();
		spell = null;
		button.interactable = true;
	}

	private void RemoveAutocast()
	{
		GameManager.Instance.Scrolls.RemoveAutoCast(this);
	}

	public void Action()
	{
		if (spell == null)
		{
			Change();
		}
		else if (Input.GetKey(KeyCode.X))
		{
			Clear(soft: true);
		}
		else if (GameManager.Instance.Scrolls.AutoCastCount.ValueInt == 0 || GameManager.Instance.Scrolls.AutocastModes.mode == AutocastModes.Mode.none)
		{
			if (cur_action == null)
			{
				spell_active();
			}
		}
		else if (GameManager.Instance.Scrolls.AutocastModes.mode == AutocastModes.Mode.careless)
		{
			if (AutoCast)
			{
				if (careful_autocast)
				{
					SetAutoMode(2);
				}
				else
				{
					RemoveAutocast();
				}
			}
			else
			{
				SetAutoMode(2);
			}
		}
		else
		{
			if (GameManager.Instance.Scrolls.AutocastModes.mode != AutocastModes.Mode.careful)
			{
				return;
			}
			if (AutoCast)
			{
				if (!careful_autocast)
				{
					SetAutoMode(1);
				}
				else
				{
					RemoveAutocast();
				}
			}
			else
			{
				SetAutoMode(1);
			}
		}
	}

	private void spell_active()
	{
		if (spell == null)
		{
			return;
		}
		float num = castTimer;
		if (cur_action != null || !spell.AvailableToCast || (AutoCast && num < GetCastRate))
		{
			return;
		}
		if (GameManager.Instance.Scrolls.OnPreCast != null)
		{
			GameManager.Instance.Scrolls.OnPreCast(spell);
		}
		if (spell.ShardsBuilding && !GameManager.Instance.Scrolls.unfilled_scrolls.Contains(this))
		{
			GameManager.Instance.Scrolls.unfilled_scrolls.Add(this);
		}
		int num2 = 1;
		if (spell.Duration.Value.ToInt() == 0)
		{
			if (AutoCast && !careful_autocast)
			{
				num2 = Mathf.FloorToInt(num / GetCastRate);
				int num3 = 0;
				for (num3 = 0; num3 < num2; num3++)
				{
					if (!spell.AvailableToCast)
					{
						break;
					}
					spell.Spend();
					SpellApply();
					if (GameManager.Instance.Scrolls.OnCast != null)
					{
						GameManager.Instance.Scrolls.OnCast(spell);
					}
					spell.UpdatePassive();
				}
				num2 = num3;
				IncreaseCasts(num2);
				if (GameManager.Instance.Scrolls.OnCastEnd != null)
				{
					GameManager.Instance.Scrolls.OnCastEnd(spell);
				}
				if (spell != null && spell.ShardsBuilding && !spell.IsMax && !GameManager.Instance.Scrolls.unfilled_scrolls.Contains(this))
				{
					GameManager.Instance.Scrolls.unfilled_scrolls.Add(this);
				}
			}
			else
			{
				spell.Spend();
				IncreaseCasts(num2);
				SpellApply();
				if (GameManager.Instance.Scrolls.OnCast != null)
				{
					GameManager.Instance.Scrolls.OnCast(spell);
				}
				spell.UpdatePassive();
				if (GameManager.Instance.Scrolls.OnCastEnd != null)
				{
					GameManager.Instance.Scrolls.OnCastEnd(spell);
				}
			}
			StopAction();
			castTimer -= (float)num2 * GetCastRate;
			return;
		}
		spell.Spend();
		cur_action = action();
		StartCoroutine(cur_action);
		if (spell != null)
		{
			if (spell.get_ingame_duration() > 0.0 && spell.TypeBehavior != SpellTypeBehavior.Instant)
			{
				active = true;
			}
			if (GameManager.Instance.Scrolls.OnCast != null)
			{
				GameManager.Instance.Scrolls.OnCast(spell);
			}
		}
	}

	public void IncreaseFakeCasts(double casts)
	{
		if (spell != null)
		{
			if (spell.IsAugment)
			{
				casts *= (double)GameManager.Instance.Scrolls.AugmentMult.ValueFloat;
			}
			Statistic.Change(Statistic.CastSpellTotal, casts);
			Statistic.Change(Statistic.CastSpellRealm, casts);
			Statistic.CastSpell.Change(casts);
			if (spell.IsAccumulated || spell.IsPersistent)
			{
				GameManager.Instance.Scrolls.AccumCastCount.Change(casts);
			}
			spell.IncreaseUses(casts);
			if (GameManager.Instance.Scrolls.OnCastAmount != null)
			{
				GameManager.Instance.Scrolls.OnCastAmount(spell, casts);
			}
		}
	}

	public void IncreaseCasts()
	{
		double duration = spell.get_duration();
		double casts = 1.0;
		if (duration > 0.0 && duration < 1.0)
		{
			casts = (int)(1.0 / (duration * 60.0 * (double)GetCastRate));
		}
		IncreaseCasts(casts);
	}

	public void IncreaseCasts(double casts)
	{
		GameManager.Instance.Scrolls.castsInSec.Add(casts);
		double num = casts;
		double num2 = casts;
		if (!GameManager.Instance.ChallengeManager.StatsIsOn() && spell.IsPersistent)
		{
			num = 0.0;
		}
		if (spell.IsPersistent)
		{
			num = num * (double)GameManager.Instance.Scrolls.PersistentMult.ValueFloat * (double)GameManager.Instance.Scrolls.PersistentGain.ValueFloat;
			num2 = num2 * (double)GameManager.Instance.Scrolls.PersistentActiveMult.ValueFloat * (double)GameManager.Instance.Scrolls.PersistentGain.ValueFloat;
		}
		else if (spell.IsAccumulated)
		{
			num *= (double)GameManager.Instance.Scrolls.AccumMult.ValueFloat;
		}
		else if (spell.IsAugment)
		{
			num *= (double)GameManager.Instance.Scrolls.AugmentMult.ValueFloat;
		}
		spell.IncreaseUses(num);
		spell.IncreaseUseThisRun(num2);
		if (spell.IsAccumulated || spell.IsPersistent)
		{
			GameManager.Instance.Scrolls.AccumCastCount.Change(num);
		}
		GameManager.Instance.Scrolls.OnRealCastAmount?.Invoke(spell, casts);
		casts = Math.Max(num, num2);
		GameManager.Instance.Scrolls.OnCastAmount?.Invoke(spell, casts);
		if (!GameManager.Instance.ChallengeManager.StatsIsOn())
		{
			if (spell != null && !spell.IsPersistent)
			{
				Statistic.CastSpell.Change(casts);
			}
		}
		else
		{
			Statistic.CastSpell.Change(casts);
			Statistic.Change(Statistic.CastSpellTotal, casts);
			Statistic.Change(Statistic.CastSpellRealm, casts);
		}
	}

	private void Delete()
	{
		if (spell != null && active)
		{
			spell.Delete();
			active = false;
		}
	}

	public void StopAction()
	{
		if (spell == null)
		{
			return;
		}
		if (spell.active)
		{
			if (spell.TypeBehavior != SpellTypeBehavior.Permanent)
			{
				Delete();
			}
			if (GameManager.Instance.Scrolls.OnPostCast != null)
			{
				GameManager.Instance.Scrolls.OnPostCast(spell);
			}
		}
		active = false;
		CDIcon.fillAmount = 0f;
		CDIcon.enabled = false;
		if (GameManager.Instance.ChallengeManager.ActiveChallenge != null)
		{
			ChangeButton.interactable = true;
		}
		if (cur_action != null)
		{
			StopCoroutine(cur_action);
			cur_action = null;
		}
	}

	public void AddProgressBuild(double dBuild, bool text = false, bool shards = true)
	{
		spell.AddProgressBuild(dBuild, shards);
		if (shards && spell.IsMax)
		{
			GameManager.Instance.Scrolls.unfilled_scrolls.Remove(this);
		}
		if (text)
		{
			text_up(dBuild);
		}
	}

	public BigNumber TakeShards(float take)
	{
		if (spell == null)
		{
			return 0.0;
		}
		BigNumber result = spell.TakeShards(take);
		if (!spell.IsMax && !GameManager.Instance.Scrolls.unfilled_scrolls.Contains(this))
		{
			GameManager.Instance.Scrolls.unfilled_scrolls.Add(this);
		}
		return result;
	}

	public double GetSpellShardCapacity()
	{
		double result = 0.0;
		if (!spell.IsMax)
		{
			result = ((!spell.LimitedCharges) ? ((double)(1000000000 - spell.chargeCount) * spell.FullBuild - spell.GetProgressBuild) : ((double)((ulong)spell.MaxChargeCount - spell.chargeCount) * spell.FullBuild - spell.GetProgressBuild));
		}
		return result;
	}

	public void ActiveChange()
	{
		Frame.color = Color.yellow;
	}

	public void DeactiveChange()
	{
		Frame.color = Color.white;
	}

	private void SpellApply()
	{
		if (spell.TypeBehavior != SpellTypeBehavior.Permanent)
		{
			spell.Apply();
		}
		else if (!spell.active)
		{
			spell.Apply();
		}
		else
		{
			spell.Update();
		}
	}

	private IEnumerator action()
	{
		SpellApply();
		CDIcon.enabled = true;
		TimeOfAction = 0f;
		float num = (float)spell.get_ingame_duration().ToDouble();
		if (num > 0f && spell.TypeBehavior != SpellTypeBehavior.Instant && GameManager.Instance.ChallengeManager.ActiveChallenge != null)
		{
			ChangeButton.interactable = false;
			if (GameManager.Instance.Scrolls.ChoosePanel.scroll == this)
			{
				GameManager.Instance.Scrolls.ChoosePanel.Close();
			}
		}
		castTimer = 0f;
		while (TimeOfAction < num)
		{
			yield return null;
			num = (float)spell.get_ingame_duration().ToDouble();
			TimeOfAction += Time.deltaTime;
			float num2 = 1f - TimeOfAction / num;
			if (num2 < 0.03f)
			{
				num2 = 0.03f;
			}
			CDIcon.fillAmount = Mathf.Lerp(CDIcon.fillAmount, num2, 14f * Time.unscaledDeltaTime);
			if (spell.TypeBehavior != SpellTypeBehavior.Instant && spell.TypeBehavior != SpellTypeBehavior.Permanent)
			{
				spell.Update();
			}
		}
		IncreaseCasts();
		spell.UpdatePassive();
		CDIcon.enabled = false;
		castTimer = GetCastRate;
		if (spell != null && GameManager.Instance.Scrolls.OnCastEnd != null)
		{
			GameManager.Instance.Scrolls.OnCastEnd(spell);
		}
		StopAction();
	}

	private void update_tip()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<b>");
		stringBuilder.Append(spell.GetNameString());
		stringBuilder.AppendLine("</b> ");
		stringBuilder.AppendLine();
		stringBuilder.Append(spell.GetTooltipDescription(TimeOfAction));
		if (GameManager.Instance.Scrolls.AutoCastCount.ValueInt > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("AutocastTooltip".Translate());
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.Append("Hotkey".Translate());
		stringBuilder.Append(": ");
		stringBuilder.Append(Keycode);
		Tip.text = stringBuilder.ToString();
	}

	public void ShowTip()
	{
		if (spell == null)
		{
			Tip.text = "EmptyScroll".Translate() + "\n \n" + "Hotkey".Translate() + ": " + Keycode;
			tip_active = true;
			Tip.transform.parent.gameObject.SetActive(value: true);
		}
		else
		{
			update_tip();
			tip_active = true;
			Tip.transform.parent.gameObject.SetActive(value: true);
		}
	}

	public void HideTip()
	{
		if (tip_active)
		{
			Tip.text = "";
			tip_active = false;
			Tip.transform.parent.gameObject.SetActive(value: false);
		}
	}

	private void text_up(BigNumber profit)
	{
		Vector3 position = base.transform.position + new Vector3(0f, 1f, 0f);
		position.z = 0f;
		GameManager.Instance.AnimatedText.TextUp(profit.ToReadableString("F1"), position, new Color(0f, 0.7058824f, 1f), 1f, 0.75f, ignoreLimits: false, ignoreTimeScale: true);
	}
}
