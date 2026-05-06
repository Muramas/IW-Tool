using System;
using TMPro;
using UnityEngine;

public class SaveLoadInfo : MonoBehaviour
{
	[SerializeField]
	private SaveLoadingCompare compare;

	[SerializeField]
	private TextMeshProUGUI label;

	private SaveData data;

	private string text;

	public string save { get; private set; }

	public DateTime SaveTime => data?.SaveTime ?? DateTime.MinValue;

	public void Open(SaveData data, string save)
	{
		this.data = data;
		this.save = save;
		SetText();
		base.gameObject.SetActive(value: true);
	}

	private void Update()
	{
		if (data != null)
		{
			UpdateText();
		}
	}

	public void SetTextColor(Color color)
	{
		if (label != null)
		{
			label.color = color;
		}
	}

	public void ResetTextColor()
	{
		if (label != null)
		{
			label.color = Color.white;
		}
	}

	private void SetText()
	{
		string text = "Prime realm".Translate();
		if (data.Realm != null)
		{
			Plane plane = GameManager.Instance.Realmcraft.Get(data.Realm.Active);
			if (plane != null)
			{
				text = plane.GetName();
			}
		}
		this.text = string.Format("SaveCompareInfo".Translate(), data.SaveTime.ToLocalTime().ToString(), text, (data.Memories != null) ? data.Memories.TotalMemories.ToReadableString("F0") : "0", data.Souls.ToReadableString("F0"), data.Hero.ToString().Translate(), Statistic.time_to_string(data.TimeTotal), "{0}");
	}

	public void UpdateText()
	{
		TimeSpan timeSpan = DateTime.UtcNow - data.SaveTime;
		label.text = string.Format(text, Statistic.time_to_string(timeSpan.TotalSeconds));
	}

	public void Load()
	{
		if (data != null)
		{
			compare.Load(data);
		}
	}
}
