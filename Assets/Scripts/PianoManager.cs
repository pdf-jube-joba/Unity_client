using System.Collections.Generic;
using UnityEngine;

public class PianoManager : MonoBehaviour
{

    private Dictionary<Key, PianoKey> _keyMap = new();

    public void Start()
    {
        // キーを事前に集めておく。
        foreach (Key key in Key.EnumAllKey())
        {
            Transform key_transform = transform.Find(key.IntoName());

            // 作ってない鍵盤がある！ => 握りつぶす。
            if (key_transform == null) continue;
            Logger.Log($"Find Object Name: {key.IntoName()}");

            var pianoKey = key_transform.GetComponent<PianoKey>();
            if (pianoKey == null)
            {
                // key はあるのに PianoKey がついてない
                Logger.LogWarning($"pianoKey Component not found on {key.IntoName()}");
                continue;
            }
            _keyMap.Add(key, pianoKey);
        }
    }

    public void OnKeyEvent(KeyEvent evt, User sender)
    {
        if (_keyMap.TryGetValue(evt.key, out var key))
        {
            if (evt.velocity == 0)
            {
                key.OnReleaseKey(sender);
            }
            else
            {
                key.OnPressKey(sender, evt.velocity);
            }
        }
    }

    public void OnPause()
    {
        foreach (var item in _keyMap)
        {
            item.Value.OnPause();
        }
    }

    public void OnResume()
    {
        // なにもしないけど一応作っておく
    }
}
