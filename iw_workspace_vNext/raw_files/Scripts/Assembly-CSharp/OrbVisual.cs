using UnityEngine;

public class OrbVisual : MonoBehaviour
{
	public ParticleSystem[] particles;

	[SerializeField]
	private bool IgnoreSettings;

	private void Awake()
	{
		particles = GetComponentsInChildren<ParticleSystem>();
	}

	private void OnEnable()
	{
		if (Settings.OrbParticles)
		{
			PlayParticles();
		}
		else
		{
			StopParticles();
		}
	}

	public void SetPassive()
	{
		GameManager.Instance.Orb.Passive = particles;
	}

	public void PlayParticles()
	{
		if (!IgnoreSettings)
		{
			for (int i = 0; i < particles.Length; i++)
			{
				particles[i].Play();
			}
		}
	}

	public void StopParticles()
	{
		if (!IgnoreSettings)
		{
			for (int i = 0; i < particles.Length; i++)
			{
				particles[i].Stop();
			}
		}
	}
}
