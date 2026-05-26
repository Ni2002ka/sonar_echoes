using TMPro;
using UnityEngine;

public class ButtonQuickEdit : MonoBehaviour
{
    public string label;
    public TextMeshProUGUI textMeshProUGUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textMeshProUGUI.text = label;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
