using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleScript : MonoBehaviour
{
    [SerializeField, Range(0.0f, 10.0f)] float lastingTime = 3.0f;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DestroySelf(lastingTime));
    }

    IEnumerator DestroySelf(float time)
    {
        yield return new WaitForSeconds(time);
        Debug.Log("destroy!");
        Destroy(gameObject);
    }
}
