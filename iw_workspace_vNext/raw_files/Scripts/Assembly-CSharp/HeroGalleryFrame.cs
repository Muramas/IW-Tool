using UnityEngine;
using UnityEngine.UI;

public class HeroGalleryFrame : GalleryFrame
{
	[SerializeField]
	private Image inActive;

	[SerializeField]
	private Color32 notActive;

	[SerializeField]
	private Color32 locked;

	public override void Open()
	{
		GameManager.Instance.Gallery.Preview.Open(Portrait, (int)GameManager.Instance.Gallery.OpenedKey);
	}

	public override void UpdateColor(bool isActivated)
	{
		if (isActivated)
		{
			Border.sprite = Choosen;
			inActive.enabled = false;
		}
		else
		{
			if (Portrait.IsAnimated)
			{
				Border.sprite = NormalAnimated;
			}
			else
			{
				Border.sprite = Normal;
			}
			inActive.enabled = true;
		}
		base.transform.localScale = Vector3.one;
		UpdateLock();
	}

	public override void UpdateLock()
	{
		base.UpdateLock();
		if (Portrait.Unlocked)
		{
			inActive.color = notActive;
		}
		else
		{
			inActive.color = locked;
		}
	}

	public override void Activate()
	{
		if (Portrait.Unlocked)
		{
			string id = Portrait.GetKey() + "#" + Portrait.ID;
			GameManager.Instance.Gallery.Activate(GameManager.Instance.Gallery.OpenedKey, id);
		}
	}
}
