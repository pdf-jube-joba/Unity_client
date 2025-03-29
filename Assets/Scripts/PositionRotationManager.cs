using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PositionRotationManager : MonoBehaviour
{

    // PlayerInput でとるとエラーになるので、ちゃんと設定すること
    [SerializeField] InputActionProperty eyepositionAction2;
    [SerializeField] InputActionProperty eyerotationAction2;

    // Awake 時にコンポーネントを持つオブジェクトを取得する
    PlayerInput _playerInput;
    NetworkManager _networkManager;

    // キーボードでの移動用
    private Vector3 _defaultPosition;

    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float lookSpeed = 0.2f;

    // inputAction をとる
    private InputAction _moveAction;
    private InputAction _lookAction;

    // マウスで動いた分の合計
    float yaw = 0f;
    float pitch = 0f;
    // WASD で動いた分の合計
    Vector3 _move = new Vector3(0, 0, 0);

    private bool isHMDConnected;

    bool isPaused = false;

    public void Awake()
    {
        if (_playerInput == null)
            _playerInput = FindAnyObjectByType<PlayerInput>();

        if (_networkManager == null)
            _networkManager = FindAnyObjectByType<NetworkManager>();

        if (_playerInput == null || _networkManager == null)
        {
            Logger.LogError("PlayerInput or NetworkManager not found.");
        }
    }

    public IEnumerator Start()
    {
        // default の取得
        _defaultPosition = transform.position;

        // キーボードの取得
        _moveAction = _playerInput.actions["Move"];
        _lookAction = _playerInput.actions["Look"];

        if (_moveAction == null || _lookAction == null)
        {
            Logger.LogError("Move or Look action not found.");
            yield break;
        }

        // VR 周りの初期化
        isHMDConnected = false;
        List<UnityEngine.XR.XRDisplaySubsystem> displays = new();
        SubsystemManager.GetSubsystems(displays);

        float timeout = 1f;
        while (timeout > 0f)
        {

            if (displays.Exists(d => d.running))
            {
                isHMDConnected = true;
                break;
            }

            yield return null;
            timeout -= Time.deltaTime;
        }

        if (isHMDConnected)
        {
            eyepositionAction2.action.Enable();
            eyerotationAction2.action.Enable();
            Logger.Log("XR InputActions enabled (Start)");
        }
        else
        {
            Logger.Log("XR Display not found.");
        }

    }

    void OnEnable()
    {
        // これをしないと、 OnBeforeRender が呼ばれない => 描画されない
        Application.onBeforeRender += OnBeforeRender;
    }

    void OnDisable()
    {
        if (!isHMDConnected)
        {
            eyepositionAction2.action.Disable();
            eyerotationAction2.action.Disable();
        }

        Application.onBeforeRender -= OnBeforeRender;
    }
    void Update()
    {
        // HMD を被っていて isPaused でも、ここの処理は飛ばしてよい。
        // <= キーボードの処理だから
        if (isPaused) return;

        // 移動量の記憶
        var moveInput = _moveAction.ReadValue<Vector2>();
        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        _move += new Vector3(move.x, 0, move.z) * Time.deltaTime * moveSpeed;

        if (!isHMDConnected)
        {
            var lookInput = _lookAction.ReadValue<Vector2>();
            yaw += lookInput.x * lookSpeed;
            pitch -= lookInput.y * lookSpeed;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
        }

        _networkManager.Send(EventStruct.FromMoveEvent(
            new MoveEvent(transform.position, transform.rotation)
        ));
    }

    // VR 用の処理だから、Pause 中でも動かさないとまずい。
    private void OnBeforeRender()
    {
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        if (isHMDConnected)
        {
            pos = eyepositionAction2.action.ReadValue<Vector3>();
            rot = eyerotationAction2.action.ReadValue<Quaternion>();
        }

        transform.position = _defaultPosition + _move + pos;

        if (isHMDConnected)
        {
            transform.rotation = rot;
        }
        else
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        }
    }

    public void OnPause()
    {
        isPaused = true;
    }

    public void OnResume()
    {
        isPaused = false;
    }

    public bool IsHMDConnected
    {
        get { return isHMDConnected; }
    }
}