using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class HeroesChoose : IClassChoose
{
	public Hero Hero;

	public Achievement UnlockAchiev;

	public override void Check()
	{
		if (Hero.Unlocked)
		{
			return;
		}
		if (UnlockAchiev == null)
		{
			AchievementCategory achievementCategory = GameManager.Instance.AchievManager.AchievList.Find((AchievementCategory x) => x.Key.ToString() == HeroName.ToString());
			if (achievementCategory == null)
			{
				return;
			}
			UnlockAchiev = achievementCategory.Row[0];
		}
		if (UnlockAchiev.Unlocked)
		{
			Hero.Unlocked = true;
		}
	}

	public override bool IsUnlocked()
	{
		return Hero.Unlocked;
	}

	public override void Init()
	{
		if (!init)
		{
			Hero = GameManager.Instance.CurrentHero.HeroPanel.HeroMap[HeroName];
			Hero.Init();
			Hero.InitUpgrades();
			init = true;
		}
	}

	public void Update()
	{
		if (Hero != null)
		{
			UpdateIcon();
		}
	}

	public override bool CheckAvailable()
	{
		bool flag = GameManager.Instance.ChallengeManager.IsChangeHero() && GameManager.Instance.Realmcraft.IsChangeHero() && Hero.RequedClasses.Contains(GameManager.Instance.CurrentHero.Hero.NameKey);
		if (flag)
		{
			flag = GameManager.Instance.CurrentHero.Hero.Level.ValueInt >= Hero.LevelReq && (Hero.Unlocked || Hero.CheckConditions()) && (Hero.Tier != 2 || GameManager.Instance.Paragon.ClassT2IsAvailable);
		}
		return flag;
	}

	public override void Choose(bool inReset = false)
	{
		if (Hero.NameKey != HeroesNames.Apprentice && GameManager.Instance.ChallengeManager.StatsIsOn() && !Hero.Unlocked)
		{
			Hero.Unlocked = true;
			UnlockAchiev.Unlock();
		}
		SetHero(inReset);
		if (Settings.AutoClose)
		{
			GameManager.Instance.CurrentHero.HeroPanel.Close();
		}
		Analytics.CustomEvent("Class", new Dictionary<string, object> { 
		{
			Hero.NameKey.ToString(),
			1
		} });
	}

	public void SetHero(bool inReset = false)
	{
		GameManager.Instance.CurrentHero.SetHero(this);
	}

	public void Restart()
	{
		if (Hero != null)
		{
			Hero.Unlocked = false;
			Hero.Restart();
			Check();
		}
	}

	public override string GetFeature()
	{
		return Hero.Feature.Translate();
	}

	public override string GetDescription()
	{
		return Hero.Description.Translate();
	}

	public override string GetDescriptionReqText()
	{
		return Hero.Descrition_req_text();
	}

	protected override void UpdateIcon()
	{
		if (GameManager.Instance.CurrentHero.Hero == Hero)
		{
			if (!activate)
			{
				IconFrame.sprite = ActiveIcon;
				Frame.gameObject.SetActive(value: true);
				base.transform.localScale = Vector3.one * 1.2f;
				activate = true;
			}
		}
		else if (activate)
		{
			IconFrame.sprite = DeactiveIcon;
			Frame.gameObject.SetActive(value: false);
			base.transform.localScale = Vector3.one;
			activate = false;
		}
	}

	public override string GetName()
	{
		return Hero.Name;
	}
}
