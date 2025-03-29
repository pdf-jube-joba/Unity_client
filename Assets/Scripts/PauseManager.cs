using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{

    // pause other components
    private NetworkManager _networkManager;
    private PianoManager _pianoManager;
    private PositionRotationManager _positionRotationManager;
    private PianoInputManager _pianoInputManager;

    private Canvas _pauseMenuVR;
    private Canvas _pauseMenuDesktop;
    private TextMeshProUGUI _pauseText;
    private Button _resumeButton;
    private Button _exitButton;

    private bool isPaused = false;

    void Start()
    {
        // pause menu desktop
        _pauseMenuDesktop = transform.Find("PauseMenuDesktop").GetComponent<Canvas>();

        _resumeButton = _pauseMenuDesktop.transform.Find("ResumeButton").GetComponent<Button>();
        _resumeButton.onClick.AddListener(Resume);

        _exitButton = _pauseMenuDesktop.transform.Find("ExitButton").GetComponent<Button>();
        _exitButton.onClick.AddListener(ExitGame);

        // pause menu VR
        _pauseMenuVR = transform.Find("PauseMenuVR").GetComponent<Canvas>();

        _pauseText = _pauseMenuVR.transform.Find("PauseText").GetComponent<TextMeshProUGUI>();

        if (_pauseMenuDesktop == null || _resumeButton == null || _exitButton == null || _pauseMenuVR == null || _pauseText == null)
        {
            Logger.LogError("PauseMenu not found.");
        }

        if (_networkManager == null)
            _networkManager = FindAnyObjectByType<NetworkManager>();

        if (_pianoManager == null)
            _pianoManager = FindAnyObjectByType<PianoManager>();

        if (_positionRotationManager == null)
            _positionRotationManager = FindAnyObjectByType<PositionRotationManager>();

        if (_pianoInputManager == null)
            _pianoInputManager = FindAnyObjectByType<PianoInputManager>();

        _pauseMenuVR.gameObject.SetActive(false);
        _pauseMenuDesktop.gameObject.SetActive(false);

        // マウスを非表示 & ロック
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {

        // Pause Menu の判断
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!isPaused) Pause();
            else Resume();
        }
    }

    public void Pause()
    {
        isPaused = true;

        // VR かどうかで表示を変える
        bool isVR = _positionRotationManager.IsHMDConnected;

        if (isVR)
        {
            _pauseMenuVR.gameObject.SetActive(true);

            // VRなら位置合わせ
            var cam = Camera.main.transform;
            _pauseMenuVR.transform.position = cam.position + cam.forward * 2.5f;
            _pauseMenuVR.transform.rotation = Quaternion.LookRotation(cam.forward, Vector3.up);
        }
        else
        {
            _pauseMenuDesktop.gameObject.SetActive(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        _networkManager.OnPause();
        _positionRotationManager.OnPause();
        _pianoInputManager.OnPause();
        _pianoManager.OnPause();
    }

    public void Resume()
    {
        isPaused = false;

        _pauseMenuVR.gameObject.SetActive(false);
        _pauseMenuDesktop.gameObject.SetActive(false);

        bool isVR = _positionRotationManager.IsHMDConnected;

        // non VR => カーソル周り
        if (!isVR)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        _networkManager.OnResume();
        _positionRotationManager.OnResume();
        _pianoInputManager.OnResume();
        _pianoManager.OnResume();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
