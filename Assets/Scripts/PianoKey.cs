using System.Collections.Generic;
using UnityEngine;

public class PianoKey : MonoBehaviour
{
    [SerializeField] private AudioClip keySound;

    private Dictionary<User, AudioSource> _activeSources = new();
    Renderer _cube_renderer;

    public void Start()
    {
        _cube_renderer = GetComponent<Renderer>();
        _cube_renderer.material.color = Color.white;
    }

    public void OnPressKey(User sender, uint velocity)
    {
        if (_activeSources.ContainsKey(sender))
        {
            // すでに鳴らしてるなら一度止める
            _activeSources[sender].Stop();
            Destroy(_activeSources[sender]);
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = keySound;
        source.volume = Mathf.Clamp01(velocity / 127f); // velocityで音量調整
        source.Play();

        _activeSources[sender] = source;

        float t = 1f - Mathf.Exp(-(float)_activeSources.Count * 0.5f);
        _cube_renderer.material.color = Color.red * t + Color.white * (1 - t);
    }

    public void OnReleaseKey(User sender)
    {

        if (_activeSources.TryGetValue(sender, out var source))
        {
            source.Stop();
            Destroy(source); // AudioSource コンポーネント削除
            _activeSources.Remove(sender);
        }

        float t = 1f - Mathf.Exp(-(float)_activeSources.Count * 0.5f);
        _cube_renderer.material.color = Color.red * t + Color.white * (1 - t);
    }

    public void OnPause()
    {
        foreach (var item in _activeSources)
        {
            item.Value.Stop();
        }
        _cube_renderer.material.color = Color.white;
    }

}
