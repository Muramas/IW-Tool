using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AffixHolder : MonoBehaviour
{
	[SerializeField]
	private List<AffixSelector> effects;

	[SerializeField]
	private Image arrow;

	private Affix selected;

	private List<Affix> all;

	public void Init(List<Affix> affixes, Affix selected = null)
	{
		all = affixes;
		this.selected = selected;
		for (int i = 0; i < effects.Count && i < affixes.Count; i++)
		{
			effects[i].Init(affixes[i], OnSelect);
		}
		for (int j = affixes.Count; j < effects.Count; j++)
		{
			effects[j].gameObject.SetActive(value: false);
		}
		arrow.enabled = false;
	}

	private void OnEnable()
	{
		if (selected != null)
		{
			OnSelect(selected);
		}
	}

	public void OnSelect(Affix affix)
	{
		selected = affix;
		foreach (AffixSelector effect in effects)
		{
			if (effect.CheckSelect(affix))
			{
				Vector3 position = arrow.transform.position;
				position.y = effect.transform.position.y;
				arrow.transform.position = position;
			}
		}
		arrow.enabled = true;
	}

	public Affix GetSelected()
	{
		return selected;
	}

	public List<Affix> GetAll()
	{
		return all;
	}

	public void SetBlocking(bool isBlocked)
	{
		foreach (AffixSelector effect in effects)
		{
			effect.SetBlocking(isBlocked);
		}
	}
}
