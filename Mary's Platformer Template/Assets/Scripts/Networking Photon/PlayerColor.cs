using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerColor : MonoBehaviour
{
    [SerializeField] private MainMenuPhoton menu;

    public static Color playerColor { get; private set; }
    //public static float playerColorInHue { get; private set; }

    private const string PlayerPrefsColorKey = "PlayerColor";

    //private float[] hueList = { 0.002777f, 0.0861f, 0.1861f, 0.2944f, 0.7944f, 0.7416f, 0.5861f, 0.5083f, 0.4611f, 0.675f };
    [SerializeField] private Color[] colorList;
    private GameObject particlePrefab = null;

    //public float GetColorInHue()
    //{
    //    return playerColorInHue;
    //}
    
    public Color GetPlayerColor()
    {
        return playerColor;
    }

    //private void Start() => SetUpColor();

    public void SetUpColor()
    {
        // load particle prefab
        particlePrefab = Resources.Load<GameObject>("ChangeColorParticle");

        Color defaultColor;
        if (!PlayerPrefs.HasKey(PlayerPrefsColorKey + "r") || !PlayerPrefs.HasKey(PlayerPrefsColorKey + "g") || !PlayerPrefs.HasKey(PlayerPrefsColorKey + "b"))
        {
            Random.InitState((int)Time.realtimeSinceStartup);
            defaultColor = colorList[Random.Range(0, colorList.Length)];
        }
        else
        {
            // convert data into color
            defaultColor = new Color (PlayerPrefs.GetFloat(PlayerPrefsColorKey + "r"), 
                PlayerPrefs.GetFloat(PlayerPrefsColorKey + "g"), 
                PlayerPrefs.GetFloat(PlayerPrefsColorKey + "b"), 1.0f);
        }
        //Color defaultColor = Color.HSVToRGB(defaultHue, 1f, 0.58f);

        // set player color
        menu.GetPlayer().GetComponent<Controller>().SetCapeColor(defaultColor);

        // set button color
        menu.GetColorButton().image.color = defaultColor;

        playerColor = defaultColor;
    }
    
    public void SavePlayerColor()
    {
        PlayerPrefs.SetFloat(PlayerPrefsColorKey + "r", playerColor.r);
        PlayerPrefs.SetFloat(PlayerPrefsColorKey + "g", playerColor.g);
        PlayerPrefs.SetFloat(PlayerPrefsColorKey + "b", playerColor.b);
    }

    /// <summary>
    /// Generate a new random color and assign to the player
    /// </summary>
    public void SetPlayerColor()
    {
        // spawn particle
        var tmp = Instantiate(particlePrefab, menu.GetPlayer().position, Quaternion.identity).GetComponent<ParticleSystem>();

        playerColor = colorList[Random.Range(0, colorList.Length)];
        //// Get a new color that only change the Hue of HSV (Hue, saturate, value, alpha)
        //var colorTmp = Color.HSVToRGB(playerColorInHue, 1f, 0.58f);
        //colorTmp.a = 1.0f;

        menu.GetColorButton().image.color = playerColor;   // the button
        menu.GetPlayer().GetComponent<Controller>().SetCapeColor(playerColor); // the player

        playerColor = playerColor;
    }

}
