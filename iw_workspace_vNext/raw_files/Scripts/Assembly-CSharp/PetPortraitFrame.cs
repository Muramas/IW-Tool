using System;
using UnityEngine;

public class PetPortraitFrame : PortraitFrameBase
{
	public Action OnChange;

	[SerializeField]
	private float scale = 1.27f;

	[SerializeField]
	private bool isFirst = true;

	public override void UpdateFrame()
	{
		if (GameManager.Instance.PetGallery.Pets.ContainsKey(GetNameKey()))
		{
			SetActive(GetNameKey());
			checkMirror();
			base.gameObject.SetActive(value: true);
		}
	}

	private PetNames GetNameKey()
	{
		return GameManager.Instance.CurrentPet.PetPanel.GetKey(isFirst);
	}

	public override void UpdateFrame(GameObject portrait)
	{
		if (Portrait != null)
		{
			if (Portrait.name == portrait.name)
			{
				return;
			}
			UnityEngine.Object.Destroy(Portrait);
		}
		Portrait = UnityEngine.Object.Instantiate(portrait, Content);
		Portrait.transform.localPosition = Vector3.zero;
		Portrait.transform.localScale = Vector3.one * scale;
		base.gameObject.SetActive(value: true);
		checkMirror();
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public void SetActive(PetNames key)
	{
		GameObject frame = GameManager.Instance.PetGallery.Pets[key].GetFrame();
		UpdateFrame(frame);
	}

	protected override void checkMirror()
	{
		Vector3 localScale = base.transform.localScale;
		localScale.x = Mathf.Abs(localScale.x) * (float)((!Settings.MirrorPet) ? 1 : (-1));
		base.transform.localScale = localScale;
	}

	protected override void subMirror()
	{
		if (!sub)
		{
			PetSlot currentPet = GameManager.Instance.CurrentPet;
			currentPet.OnMirror = (Action)Delegate.Combine(currentPet.OnMirror, new Action(checkMirror));
			sub = true;
		}
	}

	protected override void unsubMirror()
	{
		if (sub)
		{
			PetSlot currentPet = GameManager.Instance.CurrentPet;
			currentPet.OnMirror = (Action)Delegate.Remove(currentPet.OnMirror, new Action(checkMirror));
			sub = false;
		}
	}
}
