using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ItemSetIconFlash : MonoBehaviour
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private AnimationCurve curve;

	public void StartAnimation()
	{
		StopAllCoroutines();
		base.gameObject.SetActive(value: true);
		StartCoroutine(flash(0.8f));
	}

	private IEnumerator flash(float t)
	{
		for (float timer = 0f; timer < t; timer += Time.unscaledDeltaTime)
		{
			icon.color = new Color(1f, 1f, 1f, curve.Evaluate(timer / t));
			yield return null;
		}
		base.gameObject.SetActive(value: false);
	}
}
