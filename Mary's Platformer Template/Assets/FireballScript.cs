using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FireballScript : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Vector2 directionVector = new Vector2 (0.0f, 0.0f);
    [SerializeField] private SpriteRenderer renderer;
    [SerializeField] private Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
        renderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 movTmp = new Vector2(directionVector.x * Time.deltaTime * moveSpeed, directionVector.y * Time.deltaTime * moveSpeed);
        transform.DOMove(transform.position + movTmp, Time.deltaTime, false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        anim.SetTrigger("Hit");
        enabled = false;
        Destroy(gameObject, 0.5f);
    }

    public void SetDirection(Vector2 vector)
    {
        directionVector = vector.normalized;
        //transform.rotation = Quaternion.LookRotation(directionVector, Vector2.right);
        
        //get the angle from current direction facing to desired target
        float angle = Mathf.Atan2(directionVector.y, directionVector.x) * Mathf.Rad2Deg;
        //set the angle into a quaternion + sprite offset depending on initial sprite facing direction
        Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle + 2f));
        //Roatate current game object to face the target using a slerp function which adds some smoothing to the move
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1f);
    }
}
