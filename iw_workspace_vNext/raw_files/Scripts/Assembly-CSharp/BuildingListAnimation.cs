using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BuildingListAnimation : ListAnimation
{
	public float OpenSpacing;

	public float CloseSpacing = -52f;

	public GridLayoutGroup Grid;

	private bool isOpen = true;

	public override void Close()
	{
		StopAllCoroutines();
		StartCoroutine(anim(CloseSpacing, CloseSize));
		isOpen = false;
	}

	public override void Open()
	{
		StopAllCoroutines();
		StartCoroutine(anim(OpenSpacing, OpenSize));
		isOpen = true;
	}

	public void UpdateSpacing(float space)
	{
		OpenSpacing = space;
		if (isOpen)
		{
			Open();
		}
	}

	private IEnumerator anim(float toSpacing, float toSize)
	{
		float time = 0.1f;
		float dist = toSpacing - Grid.spacing.y;
		while (Grid.spacing.y != toSpacing)
		{
			float num = dist * Time.unscaledDeltaTime / time;
			Vector2 spacing = Grid.spacing;
			float num2 = spacing.y + num;
			if (Mathf.Sign(dist) == 1f)
			{
				if (num2 > toSpacing)
				{
					num2 = toSpacing;
				}
			}
			else if (num2 < toSpacing)
			{
				num2 = toSpacing;
			}
			Grid.spacing = new Vector2(spacing.x, num2);
			yield return null;
		}
		_transform.anchorMin = new Vector2(_transform.anchorMin.x, toSize);
		yield return null;
	}
}
