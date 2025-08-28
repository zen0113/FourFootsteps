using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 도르레 플랫폼에서 감지된 오브젝트 정보를 담는 구조체
/// </summary>
[System.Serializable]
public struct DetectedObject
{
    public Transform objectTransform;
    public ObjectType type;
    public float weight;
    public string objectName;

    public DetectedObject(Transform transform, ObjectType objType, float objWeight)
    {
        objectTransform = transform;
        type = objType;
        weight = objWeight;
        objectName = transform != null ? transform.name : "Unknown";
    }

    public bool IsValid => objectTransform != null;
}

/// <summary>
/// 오브젝트 타입 열거형 (우선순위: PhysicsObject > Player > Empty)
/// </summary>
public enum ObjectType
{
    Empty = 0,      // 빈 공간
    Player = 1,     // 플레이어
    PhysicsObject = 2  // 물리 오브젝트 (박스, 돌 등)
}