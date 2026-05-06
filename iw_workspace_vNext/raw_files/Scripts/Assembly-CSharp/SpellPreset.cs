using System.Collections.Generic;

public class SpellPreset
{
	public class Slot
	{
		public Spells spell;

		public int autoMode;

		public Slot(Spells key, int mode)
		{
			spell = key;
			autoMode = mode;
		}
	}

	public class Preset
	{
		public string Name;

		public List<Slot> list;

		public bool IsEmpty()
		{
			bool flag = true;
			foreach (Slot item in list)
			{
				flag = item.spell == Spells.None;
				if (!flag)
				{
					break;
				}
			}
			return flag;
		}

		public Preset()
		{
			list = new List<Slot>();
		}

		public void Add(Spells key, int mode)
		{
			list.Add(new Slot(key, mode));
		}

		public Slot Get(int id)
		{
			if (id >= list.Count)
			{
				return null;
			}
			return list[id];
		}
	}

	public Dictionary<int, Preset> Sets;

	public SpellPreset()
	{
		Sets = new Dictionary<int, Preset>();
	}

	public void Save(int id, Preset set)
	{
		if (Sets.ContainsKey(id))
		{
			Sets[id] = set;
		}
		else
		{
			Sets.Add(id, set);
		}
	}

	public Preset Get(int id)
	{
		if (Sets.ContainsKey(id))
		{
			return Sets[id];
		}
		return null;
	}
}
