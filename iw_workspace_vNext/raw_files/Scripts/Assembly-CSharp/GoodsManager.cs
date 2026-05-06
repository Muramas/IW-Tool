using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;

public class GoodsManager : Window
{
	public enum Goods
	{
		Warps = 0,
		Buffs = 1,
		Packs = 2,
		Resources = 3,
		GetMysteries = 4
	}

	public class SaveData
	{
		public int VipTier;

		public bool EVip3;

		public int Relics;

		public int Real;

		public int Spent;

		public int Sand;

		public int Nullifiers;

		public int DECore = 1;

		public List<int> Warps;

		public int GM;

		public bool CatcherPack;

		public bool ExpeditionPack;

		public bool GathererPack;

		public bool DDGame;

		public bool Catcher;

		public int CatcherUpgrade;

		public int AutoExpedition;

		public int Gatherer;

		public List<int> RPacks;

		public SpecialOffer.SaveData Special;

		public RelicDrop.SaveData RelicDrop;

		public BuffManager.BuffSave Buffs;

		public SaveData()
		{
			Warps = null;
			Special = null;
			Buffs = null;
		}
	}

	public TextMeshProUGUI RubbiesLabel;

	public VipUI VipUI;

	public MarketVisual Visual;

	public BuffGood Production;

	public BuffGood Shards;

	public BuffGood Crit;

	public BuffGood Void;

	public BuffGood Attributes;

	public BuffGood Gathering;

	public BuffGood Crafting;

	public BuffGood Praying;

	public BuffGood Mentalizing;

	public TimeWarp TimewarpTier1;

	public TimeWarp TimewarpTier2;

	public TimeWarp TimewarpTier3;

	public GetSouls GetSoulsInnapp;

	public int VipTier;

	public VipGoodTier1 Vip1;

	public VipGoodTier2 Vip2;

	public VipGoodTier3 Vip3;

	public VipGoodTier4 Vip4;

	public VipGoodTier5 Vip5;

	public VipGoodTier6 Vip6;

	public GetRubbies GetRubbiesTier1;

	public GetRubbies GetRubbiesTier2;

	public GetRubbies GetRubbiesTier3;

	public GetRubbies GetRubbiesTier4;

	public GetRubbies GetRubbiesTier5;

	public List<int> Warps;

	public int GetMysts;

	public bool CatcherPack;

	public bool ExpeditionPack;

	public bool GathererPack;

	public bool DDGame;

	public bool Catcher;

	public int CatcherUpgrade;

	public int AutoExpedition;

	public int Gatherer;

	public Discounts discounts;

	public DisableCanvas canvas;

	public SpecialOffer special;

	public CatcherManager catcherManager;

	public PromoEnter promo;

	public RelicDrop relicDrop;

	public void Init()
	{
		discounts = GameObject.FindGameObjectWithTag("Server").GetComponent<Discounts>();
		float t = 43200f;
		TimewarpTier1 = new TimeWarp("TimewarpTier1", 2, t);
		t = 129600f;
		TimewarpTier2 = new TimeWarp("TimewarpTier2", 4, t);
		t = 432000f;
		TimewarpTier3 = new TimeWarp("TimewarpTier3", 10, t);
		GetSoulsInnapp = new GetSouls("GetSouls", 20);
		t = 172800f;
		Production = new BuffGood("Buff production", 5, GameManager.Instance.BuffManager.Profit, t, "buff_production");
		Shards = new BuffGood("Buff passive shards", 5, GameManager.Instance.BuffManager.Shards, t, "buff_shards");
		Crit = new BuffGood("Buff crit chance and crit profit", 5, GameManager.Instance.BuffManager.Crit, t, "buff_crit");
		Void = new BuffGood("Buff spawn rate and bonus voidmana from Void Entities", 5, GameManager.Instance.BuffManager.Void, t, "buff_void");
		Attributes = new BuffGood("Buff attributes", 5, GameManager.Instance.BuffManager.Attributes, t, "buff_attributes");
		t = 432000f;
		Gathering = new BuffGood("Boost Gathering Skill", 5, GameManager.Instance.BuffManager.Gathering, t, "boost_gathering");
		Crafting = new BuffGood("Boost Crafting Skill", 5, GameManager.Instance.BuffManager.Crafting, t, "boost_crafting");
		Praying = new BuffGood("Boost Praying Skill", 5, GameManager.Instance.BuffManager.Praying, t, "boost_praying");
		Mentalizing = new BuffGood("Boost Mentalizing Skill", 5, GameManager.Instance.BuffManager.Mentalizing, t, "boost_mentalizing");
		GetRubbiesTier1 = new GetRubbies("GetRubbiesTier1", 50, 25);
		GetRubbiesTier2 = new GetRubbies("GetRubbiesTier2", 100, 55);
		GetRubbiesTier3 = new GetRubbies("GetRubbiesTier3", 200, 120);
		GetRubbiesTier4 = new GetRubbies("GetRubbiesTier4", 500, 350);
		GetRubbiesTier5 = new GetRubbies("GetRubbiesTier5", 1000, 700);
		Vip1 = new VipGoodTier1();
		Vip2 = new VipGoodTier2();
		Vip2.prev_tier = Vip1;
		Vip3 = new VipGoodTier3();
		Vip3.prev_tier = Vip2;
		Vip4 = new VipGoodTier4();
		Vip4.prev_tier = Vip3;
		Vip5 = new VipGoodTier5();
		Vip5.prev_tier = Vip4;
		Vip6 = new VipGoodTier6();
		Vip6.prev_tier = Vip5;
		Warps = new List<int> { 0, 0, 0 };
		if (discounts.current != null)
		{
			Visual.Sign.TurnOnRed();
		}
		else
		{
			Visual.Sign.TurnOff();
		}
		special.Init();
		relicDrop = new RelicDrop();
	}

	protected override void Update()
	{
		base.Update();
		RubbiesLabel.text = GameManager.Instance.Relics.Get().ToString();
	}

	public override void Open()
	{
		if (GameManager.Instance.UserID != 0 && GameManager.Instance.BuffManager.isAvailable())
		{
			canvas.On();
			base.gameObject.SetActive(value: true);
			special.Open();
		}
	}

	public override void Close()
	{
		canvas.Off();
		base.gameObject.SetActive(value: false);
	}

	public SaveData Save()
	{
		return new SaveData
		{
			VipTier = VipTier,
			EVip3 = Settings.VIP3Autoclick,
			Relics = GameManager.Instance.Relics.Relic.ValueInt,
			Real = GameManager.Instance.Relics.Real.ValueInt,
			Spent = GameManager.Instance.Relics.Spent.ValueInt,
			Sand = GameManager.Instance.AlterationSand.ValueInt,
			Nullifiers = GameManager.Instance.Nullifier.ValueInt,
			Warps = Warps,
			GM = GetMysts,
			CatcherPack = CatcherPack,
			ExpeditionPack = ExpeditionPack,
			GathererPack = GathererPack,
			DDGame = DDGame,
			Catcher = Catcher,
			CatcherUpgrade = CatcherUpgrade,
			RPacks = new List<int>(Visual.RPacks),
			AutoExpedition = AutoExpedition,
			Gatherer = Gatherer,
			Special = special.Save(),
			RelicDrop = relicDrop.Save(),
			Buffs = GameManager.Instance.BuffManager.Save()
		};
	}

	public void Load(SaveData data)
	{
		if (data == null)
		{
			data = new SaveData();
		}
		if (data.RPacks == null)
		{
			data.RPacks = new List<int>(7);
		}
		while (data.RPacks.Count < 8)
		{
			data.RPacks.Add(0);
		}
		Visual.RPacks = data.RPacks;
		VipTier = data.VipTier;
		Settings.VIP3Autoclick = data.EVip3;
		GameManager.Instance.Relics.Relic.SetValue(data.Relics);
		GameManager.Instance.Relics.Real.SetValue(data.Real);
		GameManager.Instance.Relics.Spent.SetValue(data.Spent);
		GameManager.Instance.AlterationSand.SetValue(data.Sand);
		GameManager.Instance.Nullifier.SetValue(data.Nullifiers);
		if (data.DECore > 0)
		{
			GameManager.Instance.Nullifier.Change(Mathf.FloorToInt((float)data.DECore * 0.6f));
		}
		if (data.Warps != null)
		{
			for (int i = 0; i < data.Warps.Count && i < Warps.Count; i++)
			{
				Warps[i] = data.Warps[i];
			}
		}
		else
		{
			for (int j = 0; j < Warps.Count; j++)
			{
				Warps[j] = 0;
			}
		}
		GetMysts = data.GM;
		CatcherPack = data.CatcherPack;
		ExpeditionPack = data.ExpeditionPack;
		GathererPack = data.GathererPack;
		DDGame = data.DDGame;
		Catcher = data.Catcher;
		CatcherUpgrade = data.CatcherUpgrade;
		AutoExpedition = data.AutoExpedition;
		Gatherer = data.Gatherer;
		special.Load(data.Special);
		relicDrop.Load(data.RelicDrop);
		GameManager.Instance.BuffManager.Load(data.Buffs);
	}

	public void Activate()
	{
		load_vip();
		catcherManager.Load(Catcher, CatcherUpgrade);
		GameManager.Instance.Resources.gatherer.Load(Gatherer, GathererPack);
	}

	private void load_vip()
	{
		OffVip();
		switch (VipTier)
		{
		case 1:
			VipUI.SetTarget(Vip1);
			Vip1.Apply();
			break;
		case 2:
			VipUI.SetTarget(Vip2);
			Vip2.Apply();
			break;
		case 3:
			VipUI.SetTarget(Vip3);
			Vip3.Apply();
			break;
		case 4:
			VipUI.SetTarget(Vip4);
			Vip4.Apply();
			break;
		case 5:
			VipUI.SetTarget(Vip5);
			Vip5.Apply();
			break;
		case 6:
			VipUI.SetTarget(Vip6);
			Vip6.Apply();
			break;
		default:
			VipUI.SetTarget(null);
			break;
		}
	}

	public void OffVip()
	{
		Vip1.Delete();
		Vip2.Delete();
		Vip3.Delete();
		Vip4.Delete();
		Vip5.Delete();
		Vip6.Delete();
		VipUI.SetTarget(null);
	}

	public void BuyExpeditionPack()
	{
		int cost = GetCost(5, Goods.Packs);
		if (GameManager.Instance.Relics.Get() < cost)
		{
			Visual.OpenReal();
			return;
		}
		GameManager.Instance.ConfirmWindow.Open(string.Format("ConfirmPurchase".Translate(), cost + " <sprite=0>"), delegate
		{
			Relics.SpendData spendData = GameManager.Instance.Relics.Spend(cost);
			ExpeditionManager.Instance.IdleInventory.GivePack();
			SendEventSpend("expedition_pack", cost);
			ServerAPI.instance.PostPurchase("packs", "expedition_pack", spendData.free, spendData.real);
		});
	}

	public void BuyNullifierCores()
	{
		int cost = GetCost(5, Goods.Packs);
		if (GameManager.Instance.Relics.Get() < cost)
		{
			Visual.OpenReal();
			return;
		}
		GameManager.Instance.ConfirmWindow.Open(string.Format("ConfirmPurchase".Translate(), cost + " <sprite=0>"), delegate
		{
			Relics.SpendData spendData = GameManager.Instance.Relics.Spend(cost);
			GameManager.Instance.Nullifier.Change(150);
			SendEventSpend("nullifier_pack", cost);
			ServerAPI.instance.PostPurchase("packs", "nullifier_pack", spendData.free, spendData.real);
		});
	}

	public void BuyGood(string key)
	{
		switch (key)
		{
		case "BuffProduction":
			BuyBuff(Production);
			break;
		case "BuffShards":
			BuyBuff(Shards);
			break;
		case "BuffCrit":
			BuyBuff(Crit);
			break;
		case "BuffVoid":
			BuyBuff(Void);
			break;
		case "BuffAtt":
			BuyBuff(Attributes);
			break;
		case "BoostGathering":
			BuyBuff(Gathering);
			break;
		case "BoostCrafting":
			BuyBuff(Crafting);
			break;
		case "BoostPraying":
			BuyBuff(Praying);
			break;
		case "BoostMentalizing":
			BuyBuff(Mentalizing);
			break;
		case "TimewarpTier1":
		{
			int cost = GetCost(TimewarpTier1, Goods.Warps);
			Relics.SpendData spend = GameManager.Instance.Relics.GetSpend(cost);
			if (spend.free >= 0 && spend.real >= 0)
			{
				ApplyGood(cost, "time_warp_1", delegate
				{
					ChangeWarps(0, 1);
				});
				ServerAPI.instance.PostPurchase("consumable", "time_warp_1", spend.free, spend.real);
			}
			break;
		}
		case "TimewarpTier2":
		{
			int cost = GetCost(TimewarpTier2, Goods.Warps);
			Relics.SpendData spend = GameManager.Instance.Relics.GetSpend(cost);
			if (spend.free >= 0 && spend.real >= 0)
			{
				ApplyGood(cost, "time_warp_2", delegate
				{
					ChangeWarps(1, 1);
				});
				ServerAPI.instance.PostPurchase("consumable", "time_warp_2", spend.free, spend.real);
			}
			break;
		}
		case "TimewarpTier3":
		{
			int cost = GetCost(TimewarpTier3, Goods.Warps);
			Relics.SpendData spend = GameManager.Instance.Relics.GetSpend(cost);
			if (spend.free >= 0 && spend.real >= 0)
			{
				ApplyGood(cost, "time_warp_3", delegate
				{
					ChangeWarps(2, 1);
				});
				ServerAPI.instance.PostPurchase("consumable", "time_warp_3", spend.free, spend.real);
			}
			break;
		}
		case "GetSouls":
		{
			int cost = GetCost(GetSoulsInnapp, Goods.GetMysteries);
			Relics.SpendData spend = GameManager.Instance.Relics.GetSpend(cost);
			if (spend.free >= 0 && spend.real >= 0)
			{
				ApplyGood(cost, "get_souls", delegate
				{
					ChangeGetMyst(1);
				});
				ServerAPI.instance.PostPurchase("consumable", "get_souls", spend.free, spend.real);
			}
			break;
		}
		case "BuffPack":
		{
			int cost = GetCost(5, Goods.Packs);
			Relics.SpendData spend = GameManager.Instance.Relics.GetSpend(cost);
			if (spend.free >= 0 && spend.real >= 0)
			{
				ApplyGood(cost, "buff pack", ActivateBuffPack);
				ServerAPI.instance.PostPurchase("packs", "buff_pack", spend.free, spend.real);
			}
			break;
		}
		case "CoinsPack":
		{
			int cost = GetCost(20, Goods.Packs);
			Relics.SpendData spend = GameManager.Instance.Relics.GetSpend(cost);
			if (spend.free >= 0 && spend.real >= 0)
			{
				ApplyGood(cost, "coins pack", delegate
				{
					GameManager.Instance.AlterationSand.Change(150);
				});
				ServerAPI.instance.PostPurchase("packs", "sand_pack", spend.free, spend.real);
			}
			break;
		}
		case "BoonsPack":
		{
			int cost = GetCost(5, Goods.Packs);
			Relics.SpendData spend = GameManager.Instance.Relics.GetSpend(cost);
			if (spend.free >= 0 && spend.real >= 0)
			{
				ApplyGood(cost, "boons pack", ActivateBoonsPack);
				ServerAPI.instance.PostPurchase("packs", "boons_pack", spend.free, spend.real);
			}
			break;
		}
		case "ReliqPack":
		{
			int cost = GetCost(5, Goods.Packs);
			Relics.SpendData spend = GameManager.Instance.Relics.GetSpend(cost);
			if (spend.free >= 0 && spend.real >= 0)
			{
				ApplyGood(cost, "reliq pack", delegate
				{
					GameManager.Instance.Familiars.AddReliquary(FamiliarChests.Rank, 5);
				});
				ServerAPI.instance.PostPurchase("packs", "reliq_pack", spend.free, spend.real);
			}
			break;
		}
		}
	}

	public void BuyResourcePack(int id)
	{
		switch (id)
		{
		case 1:
			ApplyResourcePack(5, "1", 24000);
			break;
		case 2:
			ApplyResourcePack(20, "2", 100000);
			break;
		case 3:
			ApplyResourcePack(70, "3", 400000);
			break;
		case 4:
			ApplyResourcePack(200, "4", 1200000);
			break;
		}
	}

	public void BuyVip(int tier)
	{
		switch (tier)
		{
		case 1:
			BuyVip(Vip1);
			break;
		case 2:
			BuyVip(Vip2);
			break;
		case 3:
			BuyVip(Vip3);
			break;
		case 4:
			BuyVip(Vip4);
			break;
		case 5:
			BuyVip(Vip5);
			break;
		case 6:
			BuyVip(Vip6);
			break;
		}
		Visual.checkButton();
	}

	public void BuyRubbies(int TierID)
	{
		PlatformAPI.instance.GetKred(TierID);
	}

	public void GetRubbies(string id)
	{
		switch (id)
		{
		case "1":
			GetRubbiesTier1.Apply();
			GetRelics("1");
			break;
		case "2":
			GetRubbiesTier2.Apply();
			GetRelics("2");
			break;
		case "3":
			GetRubbiesTier3.Apply();
			GetRelics("3");
			break;
		case "4":
			GetRubbiesTier4.Apply();
			GetRelics("4");
			break;
		case "5":
			GetRubbiesTier5.Apply();
			GetRelics("5");
			break;
		case "10":
			GetStarterPack();
			break;
		case "11":
			GetAdvancedPack();
			break;
		case "12":
			GetMasterPack();
			break;
		case "13":
			GetGrandMasterPack();
			break;
		case "14":
			GetFirst();
			break;
		case "15":
			GetAlteration();
			break;
		case "16":
			GetCrafting();
			break;
		case "17":
			GetReliqPack();
			break;
		}
	}

	public void SetRubbiesValuePerPack(int id, int count)
	{
		switch (id)
		{
		case 1:
			GetRubbiesTier1.EditProfit(count);
			break;
		case 2:
			GetRubbiesTier2.EditProfit(count);
			break;
		case 3:
			GetRubbiesTier3.EditProfit(count);
			break;
		case 4:
			GetRubbiesTier4.EditProfit(count);
			break;
		}
	}

	public void UseWarp(int i)
	{
		if (Warps[i] > 0)
		{
			switch (i)
			{
			case 0:
				TimewarpTier1.Apply();
				break;
			case 1:
				TimewarpTier2.Apply();
				break;
			case 2:
				TimewarpTier3.Apply();
				break;
			}
			ChangeWarps(i, -1);
		}
	}

	public void UseGetMysts()
	{
		if (GetMysts > 0)
		{
			GetSoulsInnapp.Apply();
			GetMysts--;
			Visual.GMUses.text = "Use (" + GetMysts + " left)";
		}
	}

	public void BuyGetCatcher()
	{
		if (!Catcher)
		{
			int cost = 40;
			OpenPurchase(cost, delegate
			{
				Relics.SpendData spendData = GameManager.Instance.Relics.Spend(cost);
				Catcher = true;
				catcherManager.Load(Catcher);
				SendEventBuyPack("catcher");
				ServerAPI.instance.PostPurchase("permanent", "hunter", spendData.free, spendData.real);
				Visual.UpdateCatcher();
			});
		}
	}

	public void BuyUpgradeCatcher()
	{
		if (Catcher && CatcherUpgrade <= 0)
		{
			int cost = 20;
			OpenPurchase(cost, delegate
			{
				Relics.SpendData spendData = GameManager.Instance.Relics.Spend(cost);
				CatcherUpgrade = 1;
				catcherManager.Load(Catcher, CatcherUpgrade);
				SendEventBuyPack("catcher_upgrade");
				ServerAPI.instance.PostPurchase("permanent", "hunter_upgrade", spendData.free, spendData.real);
				Visual.UpdateCatcher();
			});
		}
	}

	public void BuyAutoExpedition()
	{
		if (AutoExpedition <= 0)
		{
			int cost = 40;
			OpenPurchase(cost, delegate
			{
				Relics.SpendData spendData = GameManager.Instance.Relics.Spend(cost);
				AutoExpedition = 1;
				SendEventBuyPack("auto_expedition");
				ServerAPI.instance.PostPurchase("permanent", "expedition", spendData.free, spendData.real);
				Visual.UpdateExpedition();
			});
		}
	}

	public void BuyAutoExpeditionUpgrade()
	{
		if (AutoExpedition <= 1)
		{
			int cost = 20;
			OpenPurchase(cost, delegate
			{
				Relics.SpendData spendData = GameManager.Instance.Relics.Spend(cost);
				AutoExpedition = 2;
				SendEventBuyPack("auto_expedition_upgrade");
				ServerAPI.instance.PostPurchase("permanent", "expedition_upgrade", spendData.free, spendData.real);
				Visual.UpdateExpedition();
			});
		}
	}

	public void BuyGathererUnlock()
	{
		if (Gatherer <= 0)
		{
			int cost = 40;
			OpenPurchase(cost, delegate
			{
				Relics.SpendData spendData = GameManager.Instance.Relics.Spend(cost);
				Gatherer = 1;
				GameManager.Instance.Resources.gatherer.Load(Gatherer, GathererPack);
				SendEventBuyPack("gatherer_unlock");
				ServerAPI.instance.PostPurchase("permanent", "gatherer_unlock", spendData.free, spendData.real);
				Visual.UpdateGatherer();
			});
		}
	}

	public void BuyGathererUpgrade()
	{
		if (Gatherer <= 1)
		{
			int cost = 40;
			OpenPurchase(cost, delegate
			{
				Relics.SpendData spendData = GameManager.Instance.Relics.Spend(cost);
				Gatherer = 2;
				GameManager.Instance.Resources.gatherer.Load(Gatherer, GathererPack);
				SendEventBuyPack("gatherer_upgrade");
				ServerAPI.instance.PostPurchase("permanent", "gatherer_upgrade", spendData.free, spendData.real);
				Visual.UpdateGatherer();
			});
		}
	}

	public void GetStarterPack()
	{
		GameManager.Instance.Relics.Real.Change(60);
		ChangeWarps(0, 3);
		ChangeWarps(1, 2);
		ChangeWarps(2, 1);
		ChangeGetMyst(1);
		Visual.BoughtSpecial(0);
		SendEventBuyPack("starter");
	}

	public void GetAdvancedPack()
	{
		GameManager.Instance.Relics.Real.Change(150);
		ChangeWarps(2, 3);
		ChangeGetMyst(3);
		ActivateBuffPack();
		Visual.BoughtSpecial(1);
		SendEventBuyPack("advanced");
	}

	public void GetMasterPack()
	{
		GameManager.Instance.Relics.Real.Change(350);
		ChangeWarps(2, 3);
		ChangeGetMyst(3);
		ActivateBuffPack(2);
		ActivateResourcePack(200000);
		Visual.BoughtSpecial(2);
		SendEventBuyPack("master");
	}

	public void GetGrandMasterPack()
	{
		GameManager.Instance.Relics.Real.Change(700);
		ChangeWarps(2, 5);
		ChangeGetMyst(6);
		ActivateBuffPack(4);
		ActivateResourcePack(400000);
		GameManager.Instance.AlterationSand.Change(600);
		Visual.BoughtSpecial(3);
		SendEventBuyPack("grandmaster");
	}

	public void GetFirst()
	{
		GameManager.Instance.Relics.Real.Change(35);
		Visual.BoughtSpecial(4);
		SendEventBuyPack("first");
	}

	public void GetAlteration()
	{
		GameManager.Instance.Relics.Real.Change(80);
		GameManager.Instance.AlterationSand.Change(600);
		Visual.BoughtSpecial(5);
		SendEventBuyPack("alteration");
	}

	public void GetCrafting()
	{
		GameManager.Instance.Relics.Real.Change(80);
		ActivateResourcePack(400000);
		Gathering.Apply();
		Crafting.Apply();
		Visual.BoughtSpecial(6);
		SendEventBuyPack("crafting");
	}

	public void GetReliqPack()
	{
		GameManager.Instance.Relics.Real.Change(20);
		GameManager.Instance.Familiars.AddReliquary(FamiliarChests.Common, 20);
		GameManager.Instance.Familiars.AddReliquary(FamiliarChests.Rank, 10);
		Visual.BoughtSpecial(7);
		SendEventBuyPack("reliq pack");
	}

	public void ChangeWarps(int warp, int count)
	{
		Warps[warp] += count;
		Visual.UpdateUses();
	}

	public void ChangeGetMyst(int count)
	{
		GetMysts += count;
		Visual.UpdateUses();
	}

	public void ActivateBoosterPack()
	{
		Crafting.Apply();
		Gathering.Apply();
		Mentalizing.Apply();
		Praying.Apply();
	}

	public void ActivateBuffPack()
	{
		ActivateBuffPack(1);
	}

	public void ActivateBuffPack(int times = 1)
	{
		Production.Apply(times);
		Void.Apply(times);
		Shards.Apply(times);
		Crit.Apply(times);
		Attributes.Apply(times);
	}

	public void ActivateBoonsPack()
	{
		Temple temple = GameManager.Instance.Pantheon.temple;
		temple.Buff.Amount += 150;
		temple.XP.Amount += 60;
		temple.Power.Amount += 30;
	}

	public void ActivateResourcePack(int x)
	{
		GameManager.Instance.Craft.Red.Change(x);
		GameManager.Instance.Craft.Green.Change(x);
		GameManager.Instance.Craft.Blue.Change(x);
		GameManager.Instance.Craft.Yellow.Change(x);
	}

	private void OpenPurchase(int cost, Action buy)
	{
		if (GameManager.Instance.Relics.Get() >= cost)
		{
			GameManager.Instance.ConfirmWindow.Open(string.Format("ConfirmPurchase".Translate(), cost + " <sprite=0>"), buy);
		}
		else
		{
			Visual.OpenReal();
		}
	}

	private void BuyBuff(BuffGood buff)
	{
		int cost = GetCost(buff, Goods.Buffs);
		Relics.SpendData spend = GameManager.Instance.Relics.GetSpend(cost);
		ApplyGood(cost, buff.analyticsKey, buff.Apply);
		ServerAPI.instance.PostPurchase("consumable", buff.Name, spend.free, spend.real);
	}

	private void ApplyGood(int cost, string key, Action apply)
	{
		OpenPurchase(cost, delegate
		{
			GameManager.Instance.Relics.Spend(cost);
			apply();
			SendEventSpend(key, cost);
		});
	}

	private void ApplyResourcePack(int cost, string key, int resouces)
	{
		cost = GetCost(cost, Goods.Resources);
		OpenPurchase(cost, delegate
		{
			Relics.SpendData spendData = GameManager.Instance.Relics.Spend(cost);
			ActivateResourcePack(resouces);
			SendEventResPack(key, cost);
			ServerAPI.instance.PostPurchase("packs", "dust_" + key, spendData.free, spendData.real);
		});
	}

	private void BuyVip(VipGoodTier1 vip)
	{
		OpenPurchase(vip.Cost, delegate
		{
			Visual.ChangeOrbStates();
			Relics.SpendData spendData = GameManager.Instance.Relics.Spend(vip.Cost);
			VipTier = vip.Tier;
			vip.OnBuy();
			load_vip();
			VipUI.SetTarget(vip);
			SendEventSpend(vip.EventKey, vip.Cost);
			ServerAPI.instance.PostPurchase("permanent", "vip_" + vip.Tier, spendData.free, spendData.real);
		});
	}

	public void SendEventSpend(string key, int value)
	{
		Analytics.CustomEvent(key);
		Analytics.Transaction("key", value, "relic");
		Analytics.CustomEvent("relics", new Dictionary<string, object> { { "spend", value } });
	}

	public void SendEventResPack(string key, int value)
	{
		Analytics.CustomEvent("ResourcePack", new Dictionary<string, object> { { key, 1 } });
		Analytics.Transaction("key", value, "relic");
		Analytics.CustomEvent("relics", new Dictionary<string, object> { { "spend", value } });
	}

	public void GetRelics(string key)
	{
		Analytics.CustomEvent("RelicPack", new Dictionary<string, object> { { key, 1 } });
	}

	private void SendEventBuyPack(string key)
	{
		Analytics.CustomEvent(key);
	}

	private int GetCost(InappGood good, Goods key)
	{
		if (discounts.current == null)
		{
			return good.Cost;
		}
		Discounts.CurrentDiscount.Item item = discounts.current.items.FirstOrDefault((Discounts.CurrentDiscount.Item x) => x.name == key.ToString());
		if (item != null)
		{
			return (int)((float)good.Cost * (1f - (float)item.discount / 100f));
		}
		return good.Cost;
	}

	public int GetCost(int cost, Goods key)
	{
		if (discounts.current == null)
		{
			return cost;
		}
		Discounts.CurrentDiscount.Item item = discounts.current.items.FirstOrDefault((Discounts.CurrentDiscount.Item x) => x.name == key.ToString());
		if (item != null)
		{
			return (int)((float)cost * (1f - (float)item.discount / 100f));
		}
		return cost;
	}

	public void GiveReward(string key, int value)
	{
		if (key.Contains("Buff"))
		{
			string[] array = key.Split('.');
			GameManager.Instance.BuffManager.Give(array[1], value);
		}
		else if (key == ResourcesGame.GMyst.ToString())
		{
			ChangeGetMyst(value);
		}
		else if (key == ResourcesGame.TimeWarp1.ToString())
		{
			ChangeWarps(0, value);
		}
		if (key == ResourcesGame.TimeWarp2.ToString())
		{
			ChangeWarps(1, value);
		}
		if (key == ResourcesGame.TimeWarp3.ToString())
		{
			ChangeWarps(2, value);
		}
	}

	public void GetCatcherPack()
	{
		if (!CatcherPack)
		{
			Debug.Log("GetCatcherPack");
			CatcherPack = true;
			int num = 30;
			if (Catcher)
			{
				num += 40;
			}
			if (CatcherUpgrade == 1)
			{
				num += 20;
			}
			Catcher = true;
			CatcherUpgrade = 1;
			catcherManager.Load(Catcher, CatcherUpgrade);
			GameManager.Instance.Relics.Real.Change(num);
			GameManager.Instance.Shop.catcherManager.Unlock(11);
			SendEventBuyPack("catcher_pack");
		}
	}

	public void TakeBackCatcherPack()
	{
		if (CatcherPack)
		{
			Debug.Log("RemoveCatcherPack");
			CatcherPack = false;
			int num = 30;
			Catcher = false;
			CatcherUpgrade = 0;
			catcherManager.Load(Catcher, CatcherUpgrade);
			if (GameManager.Instance.Relics.Real.ValueInt < num)
			{
				int valueInt = GameManager.Instance.Relics.Real.ValueInt;
				GameManager.Instance.Relics.Real.Change(-valueInt);
				num -= valueInt;
				GameManager.Instance.Relics.Relic.Change(num);
			}
			else
			{
				GameManager.Instance.Relics.Real.Change(-num);
			}
			GameManager.Instance.Shop.catcherManager.Lock(11);
		}
	}

	public void GetExpeditionPack()
	{
		if (!ExpeditionPack)
		{
			Debug.Log("GetExpeditionPack");
			int num = 30;
			if (AutoExpedition >= 1)
			{
				num += 40;
			}
			if (AutoExpedition >= 2)
			{
				num += 20;
			}
			AutoExpedition = 2;
			ExpeditionPack = true;
			GameManager.Instance.Relics.Real.Change(num);
			GameManager.Instance.Interior.Backs.Unlock(30);
			SendEventBuyPack("expedition_pack");
		}
	}

	public void TakeBackExpeditionPack()
	{
		GameManager.Instance.Interior.Backs.Lock(30);
		if (ExpeditionPack)
		{
			Debug.Log("RemoveExpeditionPack");
			ExpeditionPack = false;
			AutoExpedition = 0;
			int num = 30;
			if (GameManager.Instance.Relics.Real.ValueInt < num)
			{
				int valueInt = GameManager.Instance.Relics.Real.ValueInt;
				GameManager.Instance.Relics.Real.Change(-valueInt);
				num -= valueInt;
				GameManager.Instance.Relics.Relic.Change(num);
			}
			else
			{
				GameManager.Instance.Relics.Real.Change(-num);
			}
		}
	}

	public void GetGathererPack()
	{
		if (!GathererPack)
		{
			Debug.Log("GetGathererPack");
			GathererPack = true;
			int num = 20;
			if (Gatherer >= 1)
			{
				num += 40;
			}
			if (Gatherer >= 2)
			{
				num += 40;
			}
			Gatherer = 2;
			GameManager.Instance.Relics.Real.Change(num);
			GameManager.Instance.Resources.gatherer.Load(Gatherer, GathererPack);
			SendEventBuyPack("gatherer_pack");
		}
	}

	public void TakeBackGathererPack()
	{
		if (GathererPack)
		{
			Debug.Log("RemoveGathererPack");
			GathererPack = false;
			Gatherer = 0;
			int num = 20;
			if (GameManager.Instance.Relics.Real.ValueInt < num)
			{
				int valueInt = GameManager.Instance.Relics.Real.ValueInt;
				GameManager.Instance.Relics.Real.Change(-valueInt);
				num -= valueInt;
				GameManager.Instance.Relics.Relic.Change(num);
			}
			else
			{
				GameManager.Instance.Relics.Real.Change(-num);
			}
			GameManager.Instance.Resources.gatherer.Load(Gatherer, GathererPack);
		}
	}

	public void GetDDGame()
	{
		if (DDGame)
		{
			return;
		}
		DDGame = true;
		GameManager.Instance.Relics.Real.Change(20);
		Gallery gallery = GameManager.Instance.Gallery;
		(HeroesNames, int)[] array = new(HeroesNames, int)[6]
		{
			(HeroesNames.Shaman, 4),
			(HeroesNames.Heretic, 6),
			(HeroesNames.Oni, 5),
			(HeroesNames.Archon, 4),
			(HeroesNames.Desolator, 4),
			(HeroesNames.Temporalist, 5)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(HeroesNames, int) tuple = array[i];
			HeroesNames hero = tuple.Item1;
			int id = tuple.Item2;
			HeroPortrait heroPortrait = gallery.Classes[hero].List.Find((HeroPortrait x) => x.ID == id && x.Key == hero);
			if (heroPortrait != null)
			{
				if (heroPortrait.Unlocked)
				{
					GameManager.Instance.AlterationSand.Change(heroPortrait.Cost);
				}
				else
				{
					gallery.Unlock((int)hero, id);
				}
			}
		}
		ServerRewards.ItemDetails details = new ServerRewards.ItemDetails
		{
			sprite_name = "Relic"
		};
		ServerRewards.RewardFormat item = new ServerRewards.RewardFormat
		{
			name = "Digging Down",
			description = "Thank you for purchasing Digging Down!\r\nA set of bonus character portraits has been unlocked and the following amount of Relics have been added:",
			useServer = false,
			reward_items = new List<ServerRewards.RewardItem>
			{
				new ServerRewards.RewardItem
				{
					details = details,
					quantity = 20f
				}
			}
		};
		GameManager.Instance.ServerRewards.OpenLocal(item);
		SendEventBuyPack("dd_game");
	}

	public void TakeBackDDGame()
	{
		if (!DDGame)
		{
			return;
		}
		DDGame = false;
		int num = 20;
		if (GameManager.Instance.Relics.Real.ValueInt < num)
		{
			int valueInt = GameManager.Instance.Relics.Real.ValueInt;
			GameManager.Instance.Relics.Real.Change(-valueInt);
			num -= valueInt;
			GameManager.Instance.Relics.Relic.Change(-num);
		}
		else
		{
			GameManager.Instance.Relics.Real.Change(-num);
		}
		Gallery gallery = GameManager.Instance.Gallery;
		(HeroesNames, int)[] array = new(HeroesNames, int)[6]
		{
			(HeroesNames.Shaman, 4),
			(HeroesNames.Heretic, 6),
			(HeroesNames.Oni, 5),
			(HeroesNames.Archon, 4),
			(HeroesNames.Desolator, 4),
			(HeroesNames.Temporalist, 5)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(HeroesNames, int) tuple = array[i];
			HeroesNames hero = tuple.Item1;
			int id = tuple.Item2;
			HeroPortrait heroPortrait = gallery.Classes[hero].List.Find((HeroPortrait x) => x.ID == id && x.Key == hero);
			if (heroPortrait != null && heroPortrait.Unlocked)
			{
				heroPortrait.Lock();
				gallery.Unlocked.Change(-1);
			}
		}
	}
}
