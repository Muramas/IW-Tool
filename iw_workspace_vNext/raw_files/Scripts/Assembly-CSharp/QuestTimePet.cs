using System;

public class QuestTimePet : QuestTime
{
	public QuestTimePet(int id, string description, BigNumber baseTarget, float powFactor)
		: base(id, description, baseTarget, powFactor)
	{
	}

	protected override void Activate()
	{
		base.Activate();
		PetSlot currentPet = GameManager.Instance.CurrentPet;
		currentPet.OnSelect = (Action)Delegate.Combine(currentPet.OnSelect, new Action(ResetTime));
	}

	protected override void Deactivate()
	{
		PetSlot currentPet = GameManager.Instance.CurrentPet;
		currentPet.OnSelect = (Action)Delegate.Remove(currentPet.OnSelect, new Action(ResetTime));
		base.Deactivate();
	}

	private void ResetTime()
	{
		progress = 0.0;
	}
}
