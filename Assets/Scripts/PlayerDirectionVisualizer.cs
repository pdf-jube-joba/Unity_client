using UnityEngine;

[ExecuteAlways]
public class PlayerDirectionVisualizer : MonoBehaviour
{
    public float length = 0.5f; // 矢印の長さ

    private LineRenderer _line;

    void Start()
    {
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.05f;
        _line.endWidth = 0.05f;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startColor = Color.red;
        _line.endColor = Color.red;
        _line.useWorldSpace = true;
    }

    void Update()
    {
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * length;

        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
    }
}