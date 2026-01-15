using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontDoorInteract : InteractableBase
{
    [Header("문 상태 컨트롤러")]
    [SerializeField] private FrontDoorControl doorControl;

    public override void Interact(GameObject interactor)
    {
        doorControl.InteractDoor();
    }
}
