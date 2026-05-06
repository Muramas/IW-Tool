using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrbChoose : MonoBehaviour
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private TextMeshProUGUI nameLabel;

	[SerializeField]
	private TextMeshProUGUI costLabel;

	[SerializeField]
	private Image frame;

	[SerializeField]
	private Sprite common;

	[SerializeField]
	private Sprite selected;

	[SerializeField]
	private Button button;

	private OrbManager.Orb model;

	private OrbWindow window;

	public int ID
	{
		get
		{
			if (model != null)
			{
				return model.Id;
			}
			return -1;
		}
	}

	private void OnEnable()
	{
		UpdateVisual();
	}

	public void Init(OrbManager.Orb model, OrbWindow window)
	{
		this.model = model;
		this.window = window;
		UpdateVisual();
	}

	public void UpdateVisual()
	{
		if (model != null)
		{
			icon.sprite = window.atlas.Get(model.Sprite);
			nameLabel.text = model.Name.Translate();
			costLabel.gameObject.SetActive(model.Cost > 0);
			if (!model.Unlocked)
			{
				costLabel.text = "<sprite=1>" + model.Cost;
			}
			else
			{
				costLabel.text = string.Empty;
			}
			if (GameManager.Instance.Interior.Orbs.GetSelected() == ID)
			{
				frame.sprite = selected;
			}
			else
			{
				frame.sprite = common;
			}
			HeroesNames iD = (HeroesNames)ID;
			if (GameManager.Instance.CurrentHero.HeroPanel.HeroMap.ContainsKey(iD))
			{
				button.interactable = GameManager.Instance.CurrentHero.HeroPanel.HeroMap[(HeroesNames)ID].Unlocked;
			}
			else
			{
				button.interactable = true;
			}
		}
	}

	public Sprite GetSprite()
	{
		return icon.sprite;
	}

	public void Choose()
	{
		window.OpenPreview(this);
	}
}
