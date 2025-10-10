using System;
using UnityEngine;

/// <summary>
/// [역할] 플레이어의 상호작용(E키)을 감지하고,
/// 'OnPlayerInteracted' 이벤트를 통해 이 사실을 외부에 방송합니다.
/// </summary>
public class ReactorPuzzleEvent : MonoBehaviour
{
    public static event Action<PlayerCatMovement> OnPlayerInteracted;

    private bool _isPlayerInRange = false;
    private bool _hasInteracted = false;
    private PlayerCatMovement _playerMovement;

    private void Update()
    {
        if (_isPlayerInRange && Input.GetKeyDown(KeyCode.E) && !_hasInteracted)
        {
            _hasInteracted = true;
            Debug.Log("상호작용 신호를 방송합니다.");
            OnPlayerInteracted?.Invoke(_playerMovement);
        }
    }

    // 튜토리얼 재시작 시 상호작용 상태를 리셋하는 기능
    public void ResetInteraction()
    {
        _hasInteracted = false;
    }

    #region Player Detection
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = true;
            _playerMovement = other.GetComponent<PlayerCatMovement>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = false;
            _playerMovement = null;
        }
    }
    #endregion
}