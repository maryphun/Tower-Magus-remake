using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class PlayerNameInputPhoton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInputField = null;
    [SerializeField] private TMP_Text startGameText = null;
    [SerializeField] private CollideCallback startGamePlatform = null;
    [SerializeField] private Color disabledColor = Color.gray, enabledColor = Color.white;

    public static string DisplayName { get; private set; }

    private const string PlayerPrefsNameKey = "PlayerName";

    private void Start() => SetUpInputField();

    private void SetUpInputField()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsNameKey)) { return; }

        string defaultName = PlayerPrefs.GetString(PlayerPrefsNameKey);

        nameInputField.text = defaultName;

        SetPlayerName(defaultName);
    }

    public void SetPlayerName(string name)
    {
        // if name is empty then disable start game platform's callback and change the color, if not do otherwise
        startGameText.color = startGamePlatform.SetEnabled(!string.IsNullOrEmpty(name)) ? enabledColor : disabledColor;
    }

    public void SavePlayerName()
    {
        DisplayName = nameInputField.text;

        PhotonNetwork.NickName = DisplayName;

        PlayerPrefs.SetString(PlayerPrefsNameKey, DisplayName);
    }

    public void RemoveSavedName()
    {
        PlayerPrefs.DeleteKey(PlayerPrefsNameKey);
        SetPlayerName(string.Empty);
        nameInputField.text = string.Empty;
    }
}
