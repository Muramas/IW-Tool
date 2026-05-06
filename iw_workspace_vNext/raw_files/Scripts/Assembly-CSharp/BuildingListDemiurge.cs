using UnityEngine;
using UnityEngine.UI;

public class BuildingListDemiurge : MonoBehaviour
{
	[SerializeField]
	private GridLayoutGroup list;

	[SerializeField]
	private BuildingVisual special;

	private const float spaceActive = -15f;

	private const float spaceInactive = -6.5f;
}
