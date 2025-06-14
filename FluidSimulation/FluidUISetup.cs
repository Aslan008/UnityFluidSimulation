using UnityEngine;
using UnityEngine.UI;

public class FluidUISetup : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject sliderPrefab;
    public GameObject buttonPrefab;
    public GameObject togglePrefab;
    public GameObject textPrefab;
    
    [Header("Layout Settings")]
    public Transform uiParent;
    public float spacing = 40f;
    public Vector2 startPosition = new Vector2(20, -20);
    
    void Start()
    {
        if (uiParent == null)
            uiParent = FindObjectOfType<Canvas>().transform;
            
        CreateUI();
    }
    
    void CreateUI()
    {
        Vector2 currentPos = startPosition;
        
        CreateSlider("Particle Count", "particleCountSlider", currentPos);
        currentPos.y -= spacing;
        
        CreateSlider("Viscosity", "viscositySlider", currentPos);
        currentPos.y -= spacing;
        
        CreateSlider("Stiffness", "stiffnessSlider", currentPos);
        currentPos.y -= spacing;
        
        CreateSlider("Damping", "dampingSlider", currentPos);
        currentPos.y -= spacing;
        
        CreateSlider("Gravity", "gravitySlider", currentPos);
        currentPos.y -= spacing;
        
        CreateButton("Reset", "resetButton", currentPos);
        currentPos.y -= spacing;
        
        CreateButton("Pause", "pauseButton", currentPos);
        currentPos.y -= spacing;
        
        CreateToggle("Enable Wind", "windToggle", currentPos);
        currentPos.y -= spacing;
        
        CreatePerformanceDisplay(new Vector2(Screen.width - 200, -20));
    }
    
    GameObject CreateSlider(string labelText, string name, Vector2 position)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(uiParent);
        
        RectTransform rectTransform = sliderObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(200, 20);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.6f, 1f, 1f);
        
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(sliderObj.transform);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        
        slider.targetGraphic = handleImage;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        
        CreateLabel(labelText, sliderObj.transform, new Vector2(0, 25));
        CreateValueText(name + "Text", sliderObj.transform, new Vector2(210, 0));
        
        return sliderObj;
    }
    
    GameObject CreateButton(string labelText, string name, Vector2 position)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(uiParent);
        
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(100, 30);
        
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        button.targetGraphic = buttonImage;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        Text text = textObj.AddComponent<Text>();
        text.text = labelText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        
        return buttonObj;
    }
    
    GameObject CreateToggle(string labelText, string name, Vector2 position)
    {
        GameObject toggleObj = new GameObject(name);
        toggleObj.transform.SetParent(uiParent);
        
        RectTransform rectTransform = toggleObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(200, 20);
        
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        
        GameObject background = new GameObject("Background");
        background.transform.SetParent(toggleObj.transform);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchoredPosition = new Vector2(10, 0);
        bgRect.sizeDelta = new Vector2(20, 20);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(background.transform);
        RectTransform checkRect = checkmark.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.sizeDelta = Vector2.zero;
        checkRect.anchoredPosition = Vector2.zero;
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = new Color(0.3f, 0.6f, 1f, 1f);
        
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        
        CreateLabel(labelText, toggleObj.transform, new Vector2(40, 0));
        
        return toggleObj;
    }
    
    void CreateLabel(string text, Transform parent, Vector2 position)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent);
        
        RectTransform rectTransform = labelObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(150, 20);
        
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = text;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 12;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
    }
    
    void CreateValueText(string name, Transform parent, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(100, 20);
        
        Text valueText = textObj.AddComponent<Text>();
        valueText.text = "0";
        valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        valueText.fontSize = 12;
        valueText.color = Color.white;
        valueText.alignment = TextAnchor.MiddleLeft;
    }
    
    void CreatePerformanceDisplay(Vector2 position)
    {
        GameObject perfObj = new GameObject("PerformanceDisplay");
        perfObj.transform.SetParent(uiParent);
        
        RectTransform rectTransform = perfObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(180, 60);
        
        CreateValueText("fpsText", perfObj.transform, new Vector2(0, 0));
        CreateValueText("particleActiveText", perfObj.transform, new Vector2(0, -20));
    }
}