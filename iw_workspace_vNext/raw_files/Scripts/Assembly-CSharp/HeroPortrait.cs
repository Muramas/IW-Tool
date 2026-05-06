using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HeroPortrait : Portrait
{
	public HeroesNames Key;

	public HeroPortrait(int id, HeroesNames key, string name, GameObject obj, int cost, bool free, string lore, string links, string eventData, bool isAnimated)
		: base(id, name, obj, cost, free, lore, links, eventData, isAnimated)
	{
		Key = key;
	}

	public List<HeroesNames> GetClasses()
	{
		List<HeroesNames> list = new List<HeroesNames>();
		string[] array = Links.Split(' ');
		foreach (string s in array)
		{
			list.Add((HeroesNames)int.Parse(s));
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
