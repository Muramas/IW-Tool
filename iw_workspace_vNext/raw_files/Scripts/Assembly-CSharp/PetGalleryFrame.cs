public class PetGalleryFrame : GalleryFrame
{
	public override void Open()
	{
		GameManager.Instance.PetGallery.Preview.Open(Portrait, (int)GameManager.Instance.PetGallery.OpenedKey);
	}

	public override void Activate()
	{
		if (Portrait.Unlocked)
		{
			string id = Portrait.GetKey() + "#" + Portrait.ID;
			GameManager.Instance.PetGallery.Activate(GameManager.Instance.PetGallery.OpenedKey, id);
		}
	}
}
