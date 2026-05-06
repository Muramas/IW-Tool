using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeListAnimation : ListAnimation
{
	public Sprite OpenImage;

	public Sprite CloseImage;

	public BoxCollider2D Collider;

	public Image[] hide_graphics;

	public GameObject[] hide_objects;

	public Image image;

	public bool Opened;

	public ScrollSortingDisable sorting;

	public DisableCanvas canvas;

	public override void Close()
	{
		Opened = false;
		close();
		image.sprite = CloseImage;
		Collider.enabled = false;
	}

	public override void Open()
	{
		Opened = true;
		open();
		image.sprite = OpenImage;
		Collider.enabled = true;
	}

	private void open()
	{
		_ = _transform.anchorMin;
		GameObject[] array = hide_objects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		Image[] array2 = hide_graphics;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = true;
		}
		_transform.anchorMin = new Vector2(_transform.anchorMin.x, OpenSize);
	}

	private void close()
	{
		GameObject[] array = hide_objects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		Image[] array2 = hide_graphics;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = false;
		}
		_transform.anchorMin = new Vector2(_transform.anchorMin.x, CloseSize);
	}

	protected virtual IEnumerator anim(float to, bool open)
	{
		float num = to - _transform.anchorMin.y;
		Vector2 anchorMin = _transform.anchorMin;
		float num2 = anchorMin.y + num;
		if (!open)
		{
			GameObject[] array = hide_objects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(open);
			}
		}
		if (Mathf.Sign(num) == 1f)
		{
			if (num2 > to)
			{
				num2 = to;
			}
		}
		else if (num2 < to)
		{
			num2 = to;
		}
		_transform.anchorMin = new Vector2(anchorMin.x, num2);
		yield return null;
		Image[] array2 = hide_graphics;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = open;
		}
		if (open)
		{
			GameObject[] array = hide_objects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(open);
			}
			Opened = true;
		}
	}
}
