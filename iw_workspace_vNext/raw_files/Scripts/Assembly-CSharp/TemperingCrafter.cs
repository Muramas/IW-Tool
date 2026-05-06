using System.Collections.Generic;

public class TemperingCrafter
{
	public Dictionary<string, TemperingBasket> Baskets;

	public TemperingCrafter(List<TemperingData> data)
	{
		Baskets = new Dictionary<string, TemperingBasket>();
		foreach (TemperingData datum in data)
		{
			if (!Baskets.ContainsKey(datum.Basket))
			{
				Baskets.Add(datum.Basket, new TemperingBasket());
			}
			Baskets[datum.Basket].Add(datum);
		}
	}

	public Affix GetRandom(string basket, int seed)
	{
		if (!Baskets.ContainsKey(basket))
		{
			return null;
		}
		return Baskets[basket].Get(seed);
	}

	public Dictionary<CraftResource, BigNumber> GetCost(int seed)
	{
		List<CraftResource> list = new List<CraftResource>
		{
			CraftResource.Red,
			CraftResource.Blue,
			CraftResource.Green,
			CraftResource.Yellow
		};
		Dictionary<CraftResource, BigNumber> dictionary = new Dictionary<CraftResource, BigNumber>
		{
			{
				CraftResource.Red,
				0.0
			},
			{
				CraftResource.Blue,
				0.0
			},
			{
				CraftResource.Green,
				0.0
			},
			{
				CraftResource.Yellow,
				0.0
			}
		};
		CraftResource craftResource = list[RandomSeed.Get(seed, 0, list.Count)];
		list.Remove(craftResource);
		CraftResource key = list[RandomSeed.Get(seed + 1, 0, list.Count)];
		dictionary[craftResource] = RandomSeed.Get(seed + 2, 2500, 4000);
		dictionary[key] = 5000.0 - dictionary[craftResource];
		return dictionary;
	}

	public string GetBasket(string modelId)
	{
		foreach (KeyValuePair<string, TemperingBasket> basket in Baskets)
		{
			foreach (TemperingData bonuse in basket.Value.bonuses)
			{
				if (bonuse.Id == modelId)
				{
					return bonuse.Basket;
				}
			}
		}
		return string.Empty;
	}
}
