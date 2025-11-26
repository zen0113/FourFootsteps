using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTrigger : MonoBehaviour
{
   private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어와 충돌했는지 확인
        if (collision.CompareTag("Player"))
        {
            // 씬 UI 텍스트를 "동물병원"로 변경
            SceneUITextManager sceneTextManager = FindObjectOfType<SceneUITextManager>();
            if (sceneTextManager != null)
            {
                sceneTextManager.ResetSceneText("동물병원");
                Debug.Log("[RoadCrossing] 씬 텍스트를 '동물병원'으로 변경!");
            }
            else
            {
                Debug.LogWarning("[RoadCrossing] SceneUITextManager를 찾을 수 없습니다!");
            }
        }
    }
}
