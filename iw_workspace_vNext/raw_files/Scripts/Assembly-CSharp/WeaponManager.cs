using System;
using System.Collections.Generic;
using Weapons;

public class WeaponManager
{
	public class WeaponFormat
	{
		public string ID;

		public string Name;

		public string Sprite;

		public string WD;

		public string AD;

		public string Lore;
	}

	public class WeaponSave
	{
		public int ID;

		public float Charge;

		public WeaponSave()
		{
			ID = 0;
			Charge = 0f;
		}

		public WeaponSave(int id, float charge)
		{
			ID = id;
			Charge = charge;
		}
	}

	private List<Weapon> weapons;

	private WeaponScroll scroll;

	public void Init(WeaponScroll sc)
	{
		weapons = new List<Weapon>();
		scroll = sc;
		AddWeapon(new BranchGreatCycle());
		AddWeapon(new Cataclysm());
		AddWeapon(new HeartOfTheGrave());
		AddWeapon(new Thunderbird());
		AddWeapon(new RealityPrism());
		AddWeapon(new ShardLostDimension());
		AddWeapon(new Redeemer());
		AddWeapon(new TemporalStabilizer());
		AddWeapon(new BlackBlade());
		AddWeapon(new PhilosophersStone());
		AddWeapon(new Berzerker());
		AddWeapon(new Spellstealer());
		AddWeapon(new EnchanterStaff());
		AddWeapon(new BatsSceptr());
		AddWeapon(new EmpoweredStaff());
		AddWeapon(new ShadowScryerCrystalBall());
		AddWeapon(new HeadOfTheAllEater());
		ItemSlot slot = GameManager.Instance.Craft.window.doll.GetSlot(SlotKey.Weapon);
		slot.OnEquip = (Action<Item>)Delegate.Combine(slot.OnEquip, new Action<Item>(OnEquip));
	}

	private void AddWeapon(Weapon weapon)
	{
		weapon.Init();
		GameManager.Instance.Craft.AllItems.Add(weapon);
		weapons.Add(weapon);
	}

	private void OnEquip(Item item)
	{
		if (item == null)
		{
			scroll.SetWeapon(null);
			return;
		}
		Weapon weapon = weapons.Find((Weapon x) => x.ID == item.ID);
		scroll.SetWeapon(weapon);
	}

	public void Load(WeaponSave save, List<WeaponSave> charges)
	{
		if (save == null)
		{
			return;
		}
		foreach (Weapon w in weapons)
		{
			if (w.ID == save.ID)
			{
				w.LoadCharge(save.Charge);
				continue;
			}
			if (charges != null)
			{
				WeaponSave weaponSave = charges.Find((WeaponSave x) => x.ID == w.ID);
				if (weaponSave != null)
				{
					w.LoadCharge(weaponSave.Charge);
					continue;
				}
			}
			w.Restart();
		}
	}

	public void ResetAll()
	{
		foreach (Weapon weapon in weapons)
		{
			weapon.Restart();
		}
	}

	public void RemoveEffect()
	{
		if (scroll.weapon != null)
		{
			scroll.weapon.Remove();
		}
	}

	public WeaponSave Save()
	{
		WeaponSave weaponSave = null;
		Item item = GameManager.Instance.Craft.window.doll.GetSlot(SlotKey.Weapon).Item;
		if (item != null)
		{
			weaponSave = new WeaponSave();
			weaponSave.ID = item.ID;
			weaponSave.Charge = (item as Weapon).Progress;
			if (weaponSave.Charge < 0f)
			{
				weaponSave.Charge = 0f;
			}
		}
		return weaponSave;
	}

	public List<WeaponSave> SaveCharges()
	{
		List<WeaponSave> list = null;
		Weapon weapon = scroll.weapon;
		foreach (Weapon weapon2 in weapons)
		{
			if (weapon2.Progress >= 1f && (weapon == null || weapon2.ID != weapon.ID))
			{
				if (list == null)
				{
					list = new List<WeaponSave>();
				}
				list.Add(new WeaponSave(weapon2.ID, weapon2.Progress));
			}
		}
		return list;
	}
}
