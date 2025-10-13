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
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // 범위 내에 있고, E키를 누르고, 아직 상호작용하지 않았고, 콜라이더가 활성화되어 있을 때만
        if (_isPlayerInRange &&
            Input.GetKeyDown(KeyCode.E) &&
            !_hasInteracted &&
            _collider != null &&
            _collider.enabled)
        {
            _hasInteracted = true;
            Debug.Log("✅ [ReactorPuzzleEvent] 상호작용 신호를 방송합니다.");
            OnPlayerInteracted?.Invoke(_playerMovement);
        }
    }

    /// <summary>
    /// 튜토리얼 재시작 시 상호작용 상태를 리셋하는 기능
    /// </summary>
    public void ResetInteraction()
    {
        _hasInteracted = false;
        Debug.Log("🔄 [ReactorPuzzleEvent] 상호작용 상태가 리셋되었습니다.");
    }

    #region Player Detection
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = true;
            _playerMovement = other.GetComponent<PlayerCatMovement>();
            Debug.Log("👤 [ReactorPuzzleEvent] 플레이어가 범위 내에 진입했습니다.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = false;
            _playerMovement = null;
            Debug.Log("👤 [ReactorPuzzleEvent] 플레이어가 범위를 벗어났습니다.");
        }
    }
    #endregion
}