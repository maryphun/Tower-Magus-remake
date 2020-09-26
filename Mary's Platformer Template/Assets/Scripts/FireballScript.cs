using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FireballScript : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f, lastingTime = 5f;
    [SerializeField] private Vector2 directionVector = new Vector2 (0.0f, 0.0f);
    [SerializeField] private SpriteRenderer renderer;
    [SerializeField] private Animator anim;
    [SerializeField] private Collider2D collider;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private GameObject hitParticle;

    private Color fireballColor;
    private float lastingTimeCount = 0.0f;

    //private void Awake()
    //{
    //    anim = GetComponent<Animator>();
    //    renderer = GetComponent<SpriteRenderer>();
    //}
    
    // Update is called once per frame
    void FixedUpdate()
    {
        lastingTimeCount += Time.deltaTime;

        Vector3 movTmp = new Vector2(directionVector.x * Time.deltaTime * moveSpeed, directionVector.y * Time.deltaTime * moveSpeed);
        transform.DOMove(transform.position + movTmp, Time.deltaTime, false);

        RaycastHit2D rayCast = RayCastCollision();
        if (rayCast || lastingTimeCount >= lastingTime)
        {
            if (rayCast)
            {
                transform.position = rayCast.point - (new Vector2(directionVector.x * collider.bounds.extents.x, 0.0f));

                if (rayCast.collider.CompareTag("Enemy"))
                {
                    DealDamage(rayCast.collider.gameObject);
                }
            }

            anim.SetTrigger("Hit");
            enabled = false;
            SelfDestroy();
            SpawnParticleWithColor(transform.position + movTmp, fireballColor);
            transform.DOPause();
        }
    }

    public RaycastHit2D RayCastCollision()
    {
        return Physics2D.Raycast(transform.position, directionVector, (Time.deltaTime * moveSpeed) + collider.bounds.extents.x, layerMask);
    }

    public void Initialize(Vector2 vector, Color color)
    {
        directionVector = vector.normalized;
        //transform.rotation = Quaternion.LookRotation(directionVector, Vector2.right);
        
        //get the angle from current direction facing to desired target
        float angle = Mathf.Atan2(directionVector.y, directionVector.x) * Mathf.Rad2Deg;
        //set the angle into a quaternion + sprite offset depending on initial sprite facing direction
        Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle + 2f));
        //Roatate current game object to face the target using a slerp function which adds some smoothing to the move
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1f);
        //Initialize the color
        fireballColor = color;
        renderer.material.SetColor("_ColorChangeNewCol", fireballColor);
    }

    private void SpawnParticleWithColor(Vector2 pos, Color targetColor)
    {
        GameObject obj = Instantiate(hitParticle, pos, transform.rotation);
        ParticleSystem ps = obj.GetComponentInChildren<ParticleSystem>();
        Gradient newLifetimeColorKeys = ps.colorOverLifetime.color.gradient;

        Gradient grad = new Gradient();
        grad.SetKeys(new GradientColorKey[] { new GradientColorKey(targetColor, 0.0f), new GradientColorKey(targetColor, 1.0f) },
                       new GradientAlphaKey[] { new GradientAlphaKey(0.6078f, 0.0f), new GradientAlphaKey(0.60784f, 0.16f), new GradientAlphaKey(0.0f, 0.385f) });
        newLifetimeColorKeys = grad;
        
        var colOverLifetimeModule = ps.colorOverLifetime;
        colOverLifetimeModule.color = newLifetimeColorKeys;
    }

    private void SelfDestroy()
    {
        // 0.25f is the animation length of the impact, so this fireball will be deleted after the animation
        Destroy(gameObject, 0.25f);
    }

    private void DealDamage(GameObject target)
    {
        var enemyScript = target.GetComponent<EnemyController>();
        if (enemyScript != null)
        {
            enemyScript.TakeDamage();
        }
    }
}
