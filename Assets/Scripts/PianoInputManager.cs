using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// input のうちピアノに相当する部分
// keyboard か midi でピアノの判定
public class PianoInputManager : MonoBehaviour
{

    // Awake 時にコンポーネントを持つオブジェクトを取得する
    private PlayerInput _playerInput;
    private NetworkManager _networkManager;

    public bool isUsingMidi { get; private set; }

    private Dictionary<Key, InputAction> _keyboardPianoAction;
    bool isPaused = false;

    // MIDI Piano 判定用
    void MIDICheck()
    {
        InputSystem.onDeviceChange += (device, change) =>
            {
                var midiDevice = device as Minis.MidiDevice;
                if (midiDevice == null) return;

                Logger.Log(string.Format("{0} ({1}) {2}",
                    device.description.product, midiDevice.channel, change));

                midiDevice.onWillNoteOn += OnNoteOn;
                midiDevice.onWillNoteOff += OnNoteOff;

                isUsingMidi = true;
            };
    }

    void Awake()
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

    void Start()
    {
        // MIDI デバイス確認
        MIDICheck();

        _keyboardPianoAction = new Dictionary<Key, InputAction>();
        foreach (Key key in Key.EnumAllKey())
        {
            try
            {
                var act = _playerInput.actions[key.IntoName()];
                if (act != null)
                {
                    Logger.Log($"Find Action: {key.IntoName()}");
                    act.started += cxt => _networkManager.Send(EventStruct.FromKeyEvent(new KeyEvent(key, 60)));
                    act.canceled += cxt => _networkManager.Send(EventStruct.FromKeyEvent(new KeyEvent(key, 0)));
                    _keyboardPianoAction.Add(key, act);
                }
            }
            catch (KeyNotFoundException)
            {
                // これは今は握りつぶす
            }
        }
    }

    // MIDI piano に登録する用のイベント
    public void OnNoteOn(Minis.MidiNoteControl note, float velocity)
    {
        Logger.Log($"note: {note.noteNumber} velocity: {velocity}"); // MIDI 機器はここで値を見ること
        if (isPaused) return;
        UInt16 note_number = (UInt16)note.noteNumber;
        UInt16 v = (UInt16)(velocity * 127);
        KeyEvent evt = new KeyEvent(note_number, v);
        _networkManager.Send(EventStruct.FromKeyEvent(evt));
    }

    // MIDI piano に登録する用のイベント
    public void OnNoteOff(Minis.MidiNoteControl note)
    {
        Logger.Log($"noteoff: {note.noteNumber}");
        if (isPaused) return;
        UInt16 note_number = (UInt16)note.noteNumber;
        KeyEvent evt = new KeyEvent(note_number, 0);
        _networkManager.Send(EventStruct.FromKeyEvent(evt));
    }

    public void OnPause()
    {
        isPaused = true;
    }

    public void OnResume()
    {
        isPaused = false;
    }

}