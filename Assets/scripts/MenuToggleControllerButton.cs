using UnityEngine;

public class MenuToggleControllerButton : MonoBehaviour
{
    [Tooltip("Menu root to show/hide (e.g. the 'ButtonMenu Variant' GameObject).")]
    public GameObject menuRoot;

    [Tooltip("If menuRoot is null, we try to find this object by name at runtime.")]
    public string menuRootName = "ButtonMenu Variant";

    [Tooltip("Also allow keyboard toggle in-editor (M key).")]
    public bool allowKeyboardToggle = true;

    [Tooltip("Initial menu visibility on start (only applied if menuRoot is assigned/found).")]
    public bool startVisible = true;

    void Awake()
    {
        if (menuRoot == null && !string.IsNullOrEmpty(menuRootName))
        {
            menuRoot = GameObject.Find(menuRootName);
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(startVisible);
        }
    }

    void Update()
    {
        // "Left side button" on Quest controllers is typically the left controller Menu button.
        // In OVRInput this maps to RawButton.Start.
        bool pressed =
            OVRInput.GetDown(OVRInput.RawButton.Start) ||
            (allowKeyboardToggle && Input.GetKeyDown(KeyCode.M));

        if (!pressed) return;

        if (menuRoot == null && !string.IsNullOrEmpty(menuRootName))
        {
            menuRoot = GameObject.Find(menuRootName);
        }

        if (menuRoot == null) return;
        menuRoot.SetActive(!menuRoot.activeSelf);
    }
}

