using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class OrbPreview : MonoBehaviour
{
	public TextMeshProUGUI Name;

	public Transform Content;

	public Transform OrbPosition;

	public GameObject UnlockObj;

	public GameObject ChooseObj;

	public TextMeshProUGUI Cost;

	public Image BackImage;

	private int Id;

	private OrbManager.Orb orb;

	private Action<int> onChoose;

	private Action<int> onUnlock;

	private OrbVisual current;

	public void SetChoose(Action<int> choose, Action<int> unlock)
	{
		onChoose = choose;
		onUnlock = unlock;
	}

	private void OnDisable()
	{
		if (current != null)
		{
			UnityEngine.Object.Destroy(current.gameObject);
		}
	}

	public void Open(int Id, Sprite backImg)
	{
		this.Id = Id;
		orb = GameManager.Instance.Interior.Orbs.Orbs[Id];
		string prefab = orb.Prefab;
		if (current != null)
		{
			UnityEngine.Object.Destroy(current.gameObject);
		}
		current = Resources.Load<OrbVisual>("Orbs/" + prefab);
		Vector3 localScale = current.transform.localScale;
		current = UnityEngine.Object.Instantiate(current, OrbPosition);
		current.name = prefab;
		current.transform.localScale = localScale;
		current.transform.localPosition = Vector3.zero;
		SpriteRenderer[] componentsInChildren = OrbPosition.GetComponentsInChildren<SpriteRenderer>();
		foreach (SpriteRenderer spriteRenderer in componentsInChildren)
		{
			if (spriteRenderer.sortingLayerName == "Default")
			{
				spriteRenderer.sortingOrder += 250;
			}
			else
			{
				spriteRenderer.sortingOrder += 200;
			}
			spriteRenderer.sortingLayerName = "UI";
		}
		ParticleSystem[] particles = current.particles;
		if (particles.Length != 0)
		{
			ParticleSystem[] array = particles;
			for (int i = 0; i < array.Length; i++)
			{
				Renderer component = array[i].GetComponent<Renderer>();
				if (component.sortingLayerName == "Default")
				{
					component.sortingOrder += 250;
				}
				else
				{
					component.sortingOrder += 200;
				}
				component.sortingLayerName = "UI";
			}
		}
		BackImage.sprite = backImg;
		BackImage.enabled = false;
		Name.text = orb.Name.Translate();
		RecheckUnlock();
		base.gameObject.SetActive(value: true);
		if (Settings.OrbParticles)
		{
			current.PlayParticles();
		}
	}

	private void RecheckUnlock()
	{
		if (orb.Unlocked)
		{
			UnlockObj.SetActive(value: false);
			ChooseObj.SetActive(value: true);
			return;
		}
		if (orb.Cost > 0)
		{
			Cost.text = orb.Cost.ToString();
			UnlockObj.SetActive(value: true);
		}
		else
		{
			UnlockObj.SetActive(value: false);
		}
		ChooseObj.SetActive(value: false);
	}

	public void Unlock()
	{
		if (orb != null)
		{
			if (GameManager.Instance.Interior.Orbs.Buy(Id))
			{
				Analytics.CustomEvent("orb", new Dictionary<string, object> { { Name.text, 1 } });
				onUnlock?.Invoke(Id);
				Choose();
			}
			else
			{
				GameManager.Instance.Shop.Open();
			}
		}
	}

	public void Choose()
	{
		GameManager.Instance.Interior.Orbs.Activate(Id);
		onChoose?.Invoke(Id);
		base.gameObject.SetActive(value: false);
	}
}
