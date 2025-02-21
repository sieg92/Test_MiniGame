using UnityEngine;
using System;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class ScratchManager : MonoBehaviour
{
    public MaskCamera maskCamera; // MaskCamera 참조
    public Action<float> onScratchProgressUpdated; // 긁기 진행률 업데이트 콜백
    
    public TextMeshProUGUI progressText; // 긁기 진행도 표시할 텍스트
    public Button checkProgressButton;   // 진행도 확인 버튼
    
    private void Start()
    {
        if (maskCamera == null)
        {
            Debug.LogError("MaskCamera 설정 X"); // MaskCamera가 설정되지 않은 경우 오류 메시지 출력
            return;
        }

        // MaskCamera에서 진행률 업데이트를 처리할 콜백 설정
        maskCamera.SetProgressCallback(UpdateScratchProgress);
        
        // 버튼에 클릭 이벤트 추가
        if (checkProgressButton != null)
        {
            checkProgressButton.onClick.AddListener(OnCheckProgressButtonClicked);
        }
        else
        {
            Debug.LogError("checkProgressButton null");
        }
    }

    // MaskCamera에서 호출되는 진행률 업데이트 메서드
    private void UpdateScratchProgress(float percent)
    {
        // UI 업데이트
        if (progressText != null)
        {
            progressText.text = $"{percent:F1}%";
        }
        
        Debug.Log($"긁기 진행률: {percent}%"); // 진행률 디버그 출력

        // 진행률 업데이트 시 추가적인 로직을 실행
        onScratchProgressUpdated?.Invoke(percent);

        // 긁기 진행률이 100%에 도달했을 때 처리
        if (percent >= 100f)
        {
            Debug.Log("긁기 완료!"); // 긁기 완료 메시지 출력
        }
    }
    private void OnCheckProgressButtonClicked()
    {
        StartCoroutine(CheckProgressCoroutine());
    }

    private IEnumerator CheckProgressCoroutine()
    {
        yield return new WaitForEndOfFrame();
        // 버튼 클릭 시 진행도 업데이트 요청
        maskCamera.RequestProgressUpdate();
    }
}