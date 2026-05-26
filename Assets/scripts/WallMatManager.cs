using UnityEngine;

public class WallMatManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Transform[] children;
    void Start()
    {
        children = GetComponentsInChildren<Transform>();
    }

    // Update is called once per frame
    public void changeMaterial(string newMat)
    {
        foreach (Transform t in children)
        {
            t.gameObject.tag = newMat;
        }
    }
}
