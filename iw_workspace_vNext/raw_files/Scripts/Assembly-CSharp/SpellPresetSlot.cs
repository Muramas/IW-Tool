using System;

public class SpellPresetSlot : PresetSlot
{
	public int autoMode;

	public SpellPresetSlot()
	{
	}

	public SpellPresetSlot(int item, int mode)
		: base(item)
	{
		autoMode = mode;
	}

	public Spells GetSpell()
	{
		Spells spells = Spells.None;
		if (item == -1 || !Enum.IsDefined(typeof(Spells), item))
		{
			return Spells.None;
		}
		spells = (Spells)item;
		return GameManager.Instance.SpellBook.Enhancements.Upgrade(spells);
	}

	public override bool IsEmpty()
	{
		if (item != -1)
		{
			return item == 1000;
		}
		return true;
	}

	public override string ToString()
	{
		return item + "," + autoMode;
	}

	public override void Parse(string str)
	{
		string[] array = str.Split(',');
		int num = int.Parse(array[0]);
		int num2 = int.Parse(array[1]);
		if (Enum.IsDefined(typeof(Spells), num) && num != 1000)
		{
			item = num;
			autoMode = num2;
		}
		else
		{
			item = -1;
			autoMode = 0;
		}
	}
}
