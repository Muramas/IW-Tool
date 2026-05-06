using System.Collections.Generic;
using UnityEngine;

public class HeroQuotes
{
	public List<string> quotes;

	public HeroQuotes()
	{
		quotes = new List<string>();
	}

	public HeroQuotes(List<string> str)
	{
		quotes = str;
	}

	public void Add(string s)
	{
		quotes.Add(s);
	}

	public string GetRandom(float chance)
	{
		string result = null;
		if (((chance == 1f) ? 1f : Random.Range(0f, 1f)) <= chance)
		{
			result = quotes[Random.Range(0, quotes.Count)];
		}
		return result;
	}
}
