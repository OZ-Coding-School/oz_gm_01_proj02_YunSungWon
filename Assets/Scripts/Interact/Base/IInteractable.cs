using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상호작용 가능한 오브젝트들 공용 인터페이스
/// -플레이어가 상호작용 시도시 인터렉트 호출
/// -추후 E키상호작용, 클릭 자동이동 자동상호작용 위해
/// -인터페이스 기반 설계로 결합도 감소
/// </summary>
public interface IInteractable
{
    void Interact(GameObject interactor);
}
