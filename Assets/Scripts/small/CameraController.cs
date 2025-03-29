using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Vector2 _moveInput;
    public Vector2 _lookInput;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float lookSpeed = 0.2f;
    float yaw = 0f;
    float pitch = 0f;

    private Rigidbody _rb;
    private Camera _camera;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _camera = GetComponentInChildren<Camera>();
    }
    void FixedUpdate()
    {
        // 移動（物理）
        Vector3 move = transform.forward * _moveInput.y + transform.right * _moveInput.x;
        _rb.MovePosition(_rb.position + move * moveSpeed * Time.fixedDeltaTime);
    }

    void Update()
    {
        // 視点回転
        yaw += _lookInput.x * lookSpeed;
        pitch -= _lookInput.y * lookSpeed;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        // プレイヤー本体（水平回転）
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        // カメラ本体（上下の傾き）
        _camera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
