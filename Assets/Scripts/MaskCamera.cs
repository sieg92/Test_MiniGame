using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class MaskCamera : MonoBehaviour
{
    public GameObject Dust; // 긁을 수 있는 표면 오브젝트
    public Material EraserMaterial; // 지우개 효과를 위한 머티리얼
    private RenderTexture renderTexture; // 마스크용 렌더 텍스처
    private Texture2D tex; // 픽셀 데이터를 읽기 위한 텍스처

    private Rect screenRect; // 긁기 가능한 영역
    private Action<float> progressCallback; // 진행률 콜백
    
    // 박스 콜라이더 관련 변수 추가
    private List<BoxCollider2D> scratchBoxes = new List<BoxCollider2D>();
    private int totalBoxes = 0;
    private int scratchedBoxes = 0;

    private bool requestReadPixels = false;

    private void Start()
    {
        InitializeRenderTexture();
        CalculateScreenRect();
        InitializeScratchBoxes();
        Debug.Log(Dust.GetComponent<Renderer>().material.GetTexture("_MaskTex"));
    }

    private void InitializeRenderTexture()
    {
        // 렌더 텍스처 초기화
        renderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
        renderTexture.Create();

        tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        // 카메라와 Dust 오브젝트에 렌더 텍스처 연결
        GetComponent<Camera>().targetTexture = renderTexture;
        Dust.GetComponent<Renderer>().material.SetTexture("_MaskTex", renderTexture);
        
        Debug.Log($"RenderTexture width: {renderTexture.width}, height: {renderTexture.height}");
    }
    
    // 박스 콜라이더 초기화 함수 추가
    private void InitializeScratchBoxes()
    {
        // Surface의 자식 오브젝트들의 BoxCollider 가져오기
        foreach (Transform child in Dust.transform)
        {
            BoxCollider2D boxCollider = child.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                scratchBoxes.Add(boxCollider);
                totalBoxes++;
            }
        }
        Debug.Log($"총 박스 콜라이더 수: {totalBoxes}");
    }
    
    private void CalculateScreenRect()
    {
        // Dust 오브젝트의 화면 영역 계산
        Renderer dustRenderer = Dust.GetComponent<Renderer>();
        screenRect = new Rect(
            dustRenderer.bounds.min.x,
            dustRenderer.bounds.min.y,
            dustRenderer.bounds.size.x,
            dustRenderer.bounds.size.y
        );
        Debug.Log($"ScreenRect initialized: {screenRect}"); // screenRect 값 확인
    }

    public void SetProgressCallback(Action<float> callback)
    {
        progressCallback = callback;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 viewportPoint = GetComponent<Camera>().ScreenToViewportPoint(Input.mousePosition);
        
            // viewport 좌표를 직접 사용 (0-1 범위) 
            Vector2 localPosition = new Vector2(viewportPoint.x, viewportPoint.y);
        
            // screenRect 체크는 유지
            Vector2 worldPosition = GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
            
            if (screenRect.Contains(worldPosition))
            {
                Debug.Log($"Viewport Position: {viewportPoint}");
                CutHole(localPosition);
                CheckScratchedArea();
            }
        }

        if (requestReadPixels)
        {
            ReadPixelsAndCalculateProgress();
            requestReadPixels = false;
        }
    }
    
    // 진행률 업데이트 함수 추가
    private void UpdateScratchProgress()
    {
        if (totalBoxes > 0)
        {
            float progress = (scratchedBoxes * 100f) / totalBoxes;
            progressCallback?.Invoke(progress);
            Debug.Log($"긁기 진행률: {progress}%");

            if (progress >= 100f)
            {
                Debug.Log("긁기 완료!");
            }
        }
    }

    private void CutHole(Vector2 normalizedPosition)
    {
        Debug.Log($"CutHole called at position: {normalizedPosition}");
        
        RenderTexture.active = renderTexture; // RenderTexture 활성화

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);
        if (!EraserMaterial.SetPass(0))
        {
            Debug.LogError("Failed to set material pass!");
            return;
        }
    
        float size = 50f; // 지우개 크기를 픽셀 단위로 설정
        Vector2 pixelPos = new Vector2(
            normalizedPosition.x * renderTexture.width,
            (1- normalizedPosition.y) * renderTexture.height
        );
    
        GL.Begin(GL.QUADS);
        GL.Color(Color.white);
        GL.Vertex3(pixelPos.x - size, pixelPos.y - size, 0);
        GL.Vertex3(pixelPos.x + size, pixelPos.y - size, 0);
        GL.Vertex3(pixelPos.x + size, pixelPos.y + size, 0);
        GL.Vertex3(pixelPos.x - size, pixelPos.y + size, 0);
        GL.End();
    
        GL.PopMatrix();
        RenderTexture.active = null;
    }

    private void ReadPixelsAndCalculateProgress()
    {
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        
        float percentScratched = CalculateScratchedPercent(tex);

        progressCallback?.Invoke(percentScratched);
    }

    private float CalculateScratchedPercent(Texture2D texture)
    {
        int scratchedPixels = 0;

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                if (texture.GetPixel(x, y).r == 1) scratchedPixels++;
            }
        }

        return scratchedPixels * 100f / (texture.width * texture.height);
    }
    
    private void CheckScratchedArea()
    {
        foreach (BoxCollider2D box in scratchBoxes)
        {
            if (!box.isTrigger)  // 아직 체크되지 않은 박스만 확인
            {
                // 박스 콜라이더 영역 내의 여러 지점을 샘플링
                bool isScratched = CheckBoxArea(box);
                if (isScratched)
                {
                    box.isTrigger = true;
                    scratchedBoxes++;
                    UpdateScratchProgress();
                }
            }
        }
    }
    private bool CheckBoxArea(BoxCollider2D box)
    {
        int scratchedPoints = 0;
        int checksPerAxis = 3; // 4x4 대신 3x3으로 줄여서 테스트
        float step = 1f / (checksPerAxis + 1);
    
        for (float x = step; x < 1f; x += step)
        {
            for (float y = step; y < 1f; y += step)
            {
                Vector2 worldPoint = new Vector2(
                    Mathf.Lerp(box.bounds.min.x, box.bounds.max.x, x),
                    Mathf.Lerp(box.bounds.min.y, box.bounds.max.y, y)
                );
            
                Vector2 viewportPoint = Camera.main.WorldToViewportPoint(worldPoint);
                if (IsPointScratched(viewportPoint))
                {
                    scratchedPoints++;
                }
            }
        }
    
        return (scratchedPoints / (float)(checksPerAxis * checksPerAxis)) > 0.5f;
    }

    private bool IsPointScratched(Vector2 viewportPoint)
    {
        RenderTexture.active = renderTexture;
        Vector2 pixelPos = new Vector2(
            viewportPoint.x * renderTexture.width,
            (1 - viewportPoint.y) * renderTexture.height
        );
    
        Texture2D tempTex = new Texture2D(1, 1, TextureFormat.RGB24, false);
        tempTex.ReadPixels(new Rect(pixelPos.x, pixelPos.y, 1, 1), 0, 0);
        tempTex.Apply();
    
        Color pixelColor = tempTex.GetPixel(0, 0);
        Destroy(tempTex);
    
        // 흰색에 가까운 픽셀인지 확인 (긁힌 부분)
        return pixelColor.r > 0.3f && pixelColor.g > 0.3f && pixelColor.b > 0.3f;
    }
    
    public void RequestProgressUpdate()
    {
        requestReadPixels = true;
    }
}
