using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PlayerRenderer : MonoBehaviour
{
    [Range(0.01f, 5.0f)]
    [SerializeField] private float tailTime = 2.50f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float initialTailAlpha = 0.80f;
    [Header("Required Reference")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem jumpDust;
    [SerializeField] private ParticleSystem walkDust;


    private SpriteRenderer renderer;
    private Animator animator;

    private SpriteRenderer Renderer
    {
        get
        {
            if (renderer != null) { return renderer; }
            return renderer = GetComponent<SpriteRenderer>();
        }
    }

    void Awake()
    {
        animator = GetComponent<Animator>();

        // reference warning
        if (trail == null)
        {
            Debug.LogError("trail REFERENCE NOT FOUND FOR " + gameObject.name);
        }
        if (jumpDust == null)
        {
            Debug.LogError("jumpDust REFERENCE NOT FOUND FOR " + gameObject.name);
        }
        if (walkDust == null)
        {
            Debug.LogError("walkDust REFERENCE NOT FOUND FOR " + gameObject.name);
        }
        else
        {

        }
    }

    private void Start()
    {
        // Initialize Trail Setting
        Color startColor = Renderer.material.GetColor("_ColorChangeNewCol");
        startColor.a = 0.2f;
        trail.startColor = startColor;
        trail.endColor = Renderer.material.GetColor("_ColorChangeNewCol");
        trail.sortingOrder = Renderer.sortingOrder - 2;
        trail.time = tailTime;
    }

    public void FlipSide(float direction)
    {
        if (direction == 0) { return; }
        Renderer.flipX = direction < 0;
        //switch (direction)
        //{
        //    case 1:
        //        Renderer.flipX = false;
        //        break;
        //    case -1:
        //        Renderer.flipX = true;
        //        break;
        //    default:
        //        break;
        //}
    }

    public bool FlipSide()
    {
        return Renderer.flipX;
    }
    
    public void MoveAnimation(bool boolean)
    {
        animator.SetBool("IsMoving", boolean);
    }

    public void IsGroundedAnimParameter(bool boolean)
    {
        animator.SetBool("IsGrounded", boolean);
    }

    public void ShootAnimation(bool boolean)
    {
        animator.SetBool("IsShooting", boolean);
    }
    
    public bool IsGroundedAnimParameter()
    {
        return animator.GetBool("IsGrounded");
    }

    public void CreateAfterImage()
    {
        //--- spawning new empty object, copying tranform ---
        GameObject afterImg = new GameObject("afterImg");
        afterImg.transform.position = transform.position;
        afterImg.transform.rotation = transform.rotation;
        afterImg.transform.localScale = transform.localScale;
        afterImg.gameObject.layer = 0;
        //--- copying spriterenderer ---
        SpriteRenderer tailRenderer = afterImg.AddComponent<SpriteRenderer>();
        SpriteRenderer originalRenderer = GetComponent<SpriteRenderer>();
        tailRenderer.sortingOrder = originalRenderer.sortingOrder - 1;
        tailRenderer.sortingLayerID = originalRenderer.sortingLayerID;
        tailRenderer.sprite = originalRenderer.sprite;
        tailRenderer.color = originalRenderer.color;
        tailRenderer.flipX = Renderer.flipX;
        tailRenderer.material = originalRenderer.material;
        //tailRenderer.material = originalMaterial;
        //--- initiating tail ---
        afterImg.AddComponent<Tail>();
        afterImg.GetComponent<Tail>().Initialization(tailTime, tailRenderer, initialTailAlpha);
        //--- done ---
        Destroy(afterImg, tailTime);
    }

    public void EnableTrail(bool boolean)
    {
        trail.emitting = boolean;
    }

    public void SetColor(Color colour)
    {
        // recolor material of renderer sprite
        Renderer.material.SetColor("_ColorChangeNewCol", colour);
        // recolor trail
        trail.startColor = new Color(colour.r, colour.g, colour.b, 0.1f); ;
        trail.endColor = colour;
    }

    public Color GetColor()
    {
        return Renderer.material.GetColor("_ColorChangeNewCol");
    }

    public SpriteRenderer GetRenderer()
    {
        return Renderer;
    }

    public void JumpDustPlay()
    {
        jumpDust.Play();
    }

    public void WalkDustEnable(bool boolean)
    {
        walkDust.enableEmission = boolean;
    }
}
