using System.Collections;
using UnityEngine;

// 이 스크립트는 AudioSource 컴포넌트를 필요로 합니다.
[RequireComponent(typeof(AudioSource))]
public class ProximityAudioPlayer : MonoBehaviour
{
    [Header("타겟 설정")]
    [Tooltip("플레이어 또는 소리를 들을 대상의 Transform을 연결해주세요.")]
    [SerializeField] private Transform playerTransform;

    [Header("오디오 클립 설정")]
    [Tooltip("재생할 오디오 클립들을 이곳에 추가하세요. (기침 소리 1, 2 등)")]
    [SerializeField] private AudioClip[] audioClips;

    [Header("거리 및 볼륨 설정")]
    [Tooltip("이 거리 안으로 들어와야 소리가 최대로 들립니다.")]
    [SerializeField] private float minDistance = 2f;
    [Tooltip("이 거리를 벗어나면 소리가 들리지 않습니다.")]
    [SerializeField] private float maxDistance = 15f;

    [Header("재생 간격 설정 (초)")]
    [Tooltip("최소 대기 시간")]
    [SerializeField] private float minWaitTime = 4f;
    [Tooltip("최대 대기 시간")]
    [SerializeField] private float maxWaitTime = 10f;

    private AudioSource audioSource;

    private void Awake()
    {
        // 이 오브젝트에 붙어있는 AudioSource 컴포넌트를 가져옵니다.
        audioSource = GetComponent<AudioSource>();
        // 3D 공간감을 위해 Spatial Blend를 1로 설정합니다. (선택사항)
        audioSource.spatialBlend = 1.0f;
    }

    private void Start()
    {
        // 플레이어가 설정되었고, 오디오 클립이 하나 이상 있을 때만 코루틴을 시작합니다.
        if (playerTransform != null && audioClips.Length > 0)
        {
            StartCoroutine(PlaySoundRoutine());
        }
        else
        {
            Debug.LogError("Player Transform 또는 Audio Clips가 설정되지 않았습니다!");
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 플레이어와 이 오브젝트 사이의 거리를 계산합니다.
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 거리에 따라 볼륨을 계산합니다.
        // InverseLerp를 사용해 minDistance일 때 1, maxDistance일 때 0이 되도록 정규화합니다.
        float normalizedVolume = Mathf.InverseLerp(maxDistance, minDistance, distance);

        // 계산된 볼륨을 AudioSource에 적용합니다.
        audioSource.volume = normalizedVolume;
    }

    private IEnumerator PlaySoundRoutine()
    {
        // 게임이 실행되는 동안 무한히 반복합니다.
        while (true)
        {
            // minWaitTime과 maxWaitTime 사이의 랜덤한 시간만큼 기다립니다.
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            // 재생할 오디오 클립을 랜덤으로 선택합니다.
            int randomIndex = Random.Range(0, audioClips.Length);
            AudioClip clipToPlay = audioClips[randomIndex];

            // 선택된 클립을 재생합니다.
            // PlayOneShot을 사용하면 현재 볼륨으로 소리를 한 번 재생하며, 다른 소리와 겹칠 수 있습니다.
            audioSource.PlayOneShot(clipToPlay);
        }
    }
}