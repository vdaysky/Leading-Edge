using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class MainMenuScripts : MonoBehaviour
{
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform target;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject creditsMenu;

    [SerializeField] private AudioMixer mainMixer;
    private GameObject activeMenu;
    private const float rotationSpeedModifier = 3f;


    // Start is called before the first frame update
    void Start()
    {
        activeMenu = mainMenu;
    }

    // Update is called once per frame
    void Update()
    {
        mainCamera.LookAt(target);
        mainCamera.Translate(Vector3.right * Time.deltaTime * rotationSpeedModifier);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Debug.Log("This should work but not in editor");
        Application.Quit();
    }

    public void BackToMainMenu()
    {
        activeMenu.SetActive(false);
        mainMenu.SetActive(true);
        activeMenu = mainMenu;
    }

    public void OpenSettings()
    {
        activeMenu.SetActive(false);
        settingsMenu.SetActive(true);
        activeMenu = settingsMenu;
    }

    public void OpenCredits()
    {
        activeMenu.SetActive(false);
        creditsMenu.SetActive(true);
        activeMenu = creditsMenu;
    }

    //volumeParam
    public void VolumeChange(float volume)
    {
        mainMixer.SetFloat("volumeParam", volume);
    }
}
