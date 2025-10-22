using UnityEngine;

// TutorialManager의 UnityEventCodeExecutorTutorial을
// 비파괴 객체의 서비스를 한 방식으로 호출하기 위한 ScriptableObject 기반 커맨드 패턴
public abstract class ServiceAction : ScriptableObject
{
    // 필요한 경우 공통 안전장치/로그도 여기서 처리 가능
    public abstract void Execute();
}