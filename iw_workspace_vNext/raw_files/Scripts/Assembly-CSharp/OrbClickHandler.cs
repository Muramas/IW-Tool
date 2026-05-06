using UnityEngine;

public class OrbClickHandler : MonoBehaviour
{
	private int click_count_per_frame;

	private float click_timer;

	private float click_timer_inteval = 1f;

	private void Update()
	{
		if (click_timer > click_timer_inteval)
		{
			click_timer -= click_timer_inteval;
			click_count_per_frame = 0;
		}
		click_timer += Time.unscaledDeltaTime;
	}

	private void OnMouseDown()
	{
		if (click_count_per_frame < 25)
		{
			click_count_per_frame++;
			GameManager.Instance.Orb.Click();
		}
	}
}
