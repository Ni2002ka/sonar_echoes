using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class HMSoundSource : MonoBehaviour
{
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator AcousticsUpdate()
    {
        while (true)
        {
            RaycastHit hitInfo;
            float maxDistance = 50f;
            Physics.Raycast(new Ray(transform.position, transform.forward), out hitInfo, maxDistance);
            var echoSource = hitInfo.collider.gameObject.AddComponent<AudioSource>();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
