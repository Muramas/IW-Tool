using System;
using System.Collections.Generic;
using CardGame;
using UnityEngine;

public class EasterManager : EventController
{
	public class LootEgg : LootItem
	{
		public LootEgg(string n, int id)
			: base(n, id)
		{
		}

		public override void Give()
		{
			GameManager.Instance.Event.Eggs.Change((int)Amount);
			GameManager.Instance.Event.AddCollectedCurrency("Eggs", (int)Amount);
		}

		public override Sprite GetIcon()
		{
			return instance.eggIcon;
		}

		public override string GetText(bool isShort = true)
		{
			return "+ " + Amount + " " + "Easter egg".Translate();
		}
	}

	public static EasterManager instance;

	[SerializeField]
	private MobStats rabbit;

	[SerializeField]
	private Sprite eggIcon;

	protected override void OnEnable()
	{
		collectedThreshold = new List<int> { 300, 600, 900, 1200, 1500, 1800 };
		base.OnEnable();
		instance = this;
		if (!(ExpeditionManager.Instance == null))
		{
			LootSystem loot = ExpeditionManager.Instance.Loot;
			loot.OnDrop = (Func<List<LootItem>, MobStats, int, int, List<LootItem>>)Delegate.Combine(loot.OnDrop, new Func<List<LootItem>, MobStats, int, int, List<LootItem>>(OnDrop));
			LootSystem loot2 = ExpeditionManager.Instance.Loot;
			loot2.OnDropAuto = (Func<List<LootItem>, MobStats, int, int, List<LootItem>>)Delegate.Combine(loot2.OnDropAuto, new Func<List<LootItem>, MobStats, int, int, List<LootItem>>(OnDrop));
			ExpeditionManager.Instance.Alerts.ActivateEasterAlert();
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (!(ExpeditionManager.Instance == null))
		{
			LootSystem loot = ExpeditionManager.Instance.Loot;
			loot.OnDrop = (Func<List<LootItem>, MobStats, int, int, List<LootItem>>)Delegate.Remove(loot.OnDrop, new Func<List<LootItem>, MobStats, int, int, List<LootItem>>(OnDrop));
			LootSystem loot2 = ExpeditionManager.Instance.Loot;
			loot2.OnDropAuto = (Func<List<LootItem>, MobStats, int, int, List<LootItem>>)Delegate.Remove(loot2.OnDropAuto, new Func<List<LootItem>, MobStats, int, int, List<LootItem>>(OnDrop));
			ExpeditionManager.Instance.Alerts.DeactivateEasterAlert();
		}
	}

	private List<LootItem> OnDrop(List<LootItem> drop, MobStats mob, int characterLvl, int mobLvl)
	{
		if (ExpeditionManager.Instance.Alerts.CurrentAlertType == AlertType.EasterAlert && mob == rabbit)
		{
			int num = UnityEngine.Random.Range(1, 3);
			LootEgg item = new LootEgg("Easter egg", 999)
			{
				Amount = (ulong)num
			};
			drop.Add(item);
		}
		return drop;
	}

	public override void OnReward(int step)
	{
		switch (step)
		{
		case 0:
		case 2:
		case 4:
			GameManager.Instance.Familiars.AddReliquary(FamiliarChests.Psychic);
			break;
		case 1:
			GameManager.Instance.Shop.Warps[0]++;
			break;
		case 3:
			GameManager.Instance.BuffManager.AutoExpedition.Activate(86400f);
			break;
		case 5:
			GameManager.Instance.Shop.GetMysts++;
			break;
		}
	}
}
