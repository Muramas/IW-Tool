using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScrollSortingDisable : MonoBehaviour
{
	public GridLayoutGroup grid;

	public ContentSizeFitter fitter;

	private void OnEnable()
	{
		On();
	}

	public void On()
	{
		grid.enabled = true;
		fitter.enabled = true;
		if (base.gameObject.activeInHierarchy)
		{
			StartCoroutine(off());
		}
	}

	private IEnumerator off()
	{
		yield return null;
		grid.enabled = false;
		fitter.enabled = false;
	}
}
