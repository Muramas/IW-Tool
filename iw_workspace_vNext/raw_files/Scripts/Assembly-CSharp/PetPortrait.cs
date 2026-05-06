using System.Collections.Generic;
using UnityEngine;

public class PetPortrait : Portrait
{
	public PetNames Key;

	public PetPortrait(int id, PetNames key, string name, GameObject obj, int cost, bool free, string lore, string links, string eventData, bool isAnimated)
		: base(id, name, obj, cost, free, lore, links, eventData, isAnimated)
	{
		Key = key;
	}

	public List<PetNames> GetPets()
	{
		List<PetNames> list = new List<PetNames>();
		string[] array = Links.Split(' ');
		foreach (string s in array)
		{
			list.Add((PetNames)int.Parse(s));
		}
		return list;
	}

	public override int GetKey()
	{
		return (int)Key;
	}

	public override string GetKeyString()
	{
		return GetKey() + "#" + ID;
	}
}
