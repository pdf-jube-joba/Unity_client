using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Linq;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MoveEvent
{
    public float pos_x, pos_y, pos_z;
    public float ori_x, ori_y, ori_z;

    public static readonly int MOVE_EVENT_SIZE = Marshal.SizeOf<MoveEvent>();

    public MoveEvent(Vector3 vec, Quaternion ori)
    {
        pos_x = vec.x;
        pos_y = vec.y;
        pos_z = vec.z;

        // Quaternion → Euler 変換（degree）
        Vector3 euler = ori.eulerAngles;
        ori_x = euler.x;
        ori_y = euler.y;
        ori_z = euler.z;
    }

    public Vector3 Pos()
    {
        return new Vector3(pos_x, pos_y, pos_z);
    }

    public Quaternion Ori()
    {
        return Quaternion.Euler(ori_x, ori_y, ori_z);
    }

    public static MoveEvent ReadByte(BinaryReader reader)
    {
        var m = new MoveEvent { };
        m.pos_x = reader.ReadSingle();
        m.pos_y = reader.ReadSingle();
        m.pos_z = reader.ReadSingle();
        m.ori_x = reader.ReadSingle();
        m.ori_y = reader.ReadSingle();
        m.ori_z = reader.ReadSingle();
        return m;
    }

    public void WriteByte(BinaryWriter writer)
    {
        writer.Write(pos_x);
        writer.Write(pos_y);
        writer.Write(pos_z);
        writer.Write(ori_x);
        writer.Write(ori_y);
        writer.Write(ori_z);
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Key
{
    public UInt16 note;

    public static int MAX_KEY = 87;

    // 国際式（ Wikipedia より）
    //  0 = C-1 = 下三点ハ（＝ド）
    // 69 = A4 = 440 Hz
    //  87 => F7
    // MIDI 機器ごとに対応が異なる可能性があるため、PlayerInput 時は名前に気を付けること
    public string IntoName()
    {

        int octave = note / 12 - 1;
        int pitchClass = note % 12;

        string[] pitchNames = {
            "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
        };

        if (pitchClass < 0 || pitchClass >= pitchNames.Length) return "Unknown";

        string noteName = pitchNames[pitchClass] + octave;

        return "Note" + noteName;
    }

    public static IEnumerable EnumAllKey()
    {
        foreach (UInt16 key_num in Enumerable.Range((UInt16)0, (UInt16)MAX_KEY))
        {
            var key = new Key { note = key_num };
            yield return key;
        }
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct KeyEvent
{
    public Key key;
    public UInt16 velocity;
    public static readonly int KEY_EVENT_SIZE = Marshal.SizeOf<KeyEvent>();

    public static KeyEvent ReadByte(BinaryReader reader)
    {
        UInt16 note = reader.ReadUInt16();
        UInt16 velocity = reader.ReadUInt16();
        return new KeyEvent(note, velocity);
    }

    public void WriteByte(BinaryWriter writer)
    {
        writer.Write(key.note);
        writer.Write(velocity);
    }

    public KeyEvent(Key _key, UInt16 _velocity)
    {
        if (_velocity < 128)
        {
            key = _key;
            velocity = _velocity;
        }
        else
        {
            throw new Exception($"invalid construction of keyevent velocity:{_velocity}");
        }
    }

    public KeyEvent(UInt16 note_in, UInt16 velocity_in)
    {
        if (note_in < Key.MAX_KEY && velocity_in < 128)
        {
            key = new Key { note = note_in };
            velocity = velocity_in;
        }
        else
        {
            throw new Exception($"invalid construction of keyevent note:{note_in}, velocity:{velocity_in}");
        }
    }
}

[Serializable]
public enum EventKind : byte
{
    Join = 0,
    DisConnect = byte.MaxValue,
    Move = 2,
    Key = 3,
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct User
{
    public UInt32 user;

    public User(UInt32 u)
    {
        user = u;
    }

    public static readonly int USER_SIZE = Marshal.SizeOf<User>();

    public override bool Equals(object obj)
    {
        return obj is User other && this.user == other.user;
    }
    public override int GetHashCode()
    {
        return user.GetHashCode();
    }

    public static bool operator ==(User a, User b)
    {
        return a.user == b.user;
    }

    public static bool operator !=(User a, User b)
    {
        return a.user != b.user;
    }
}

public class EventStruct
{
    public EventKind eventkind;
    public MoveEvent move;
    public KeyEvent key;

    public static readonly int EVENT_SIZE = Math.Max(KeyEvent.KEY_EVENT_SIZE, MoveEvent.MOVE_EVENT_SIZE) + 1;

    public static EventStruct FromMoveEvent(MoveEvent m)
    {
        var e = new EventStruct();
        e.eventkind = EventKind.Move;
        e.move = m;
        return e;
    }

    public static EventStruct FromKeyEvent(KeyEvent m)
    {
        var e = new EventStruct();
        e.eventkind = EventKind.Key;
        e.key = m;
        return e;
    }

    public override string ToString()
    {
        return eventkind == EventKind.Move
            ? $"[Move] pos=({move.pos_x},{move.pos_y},{move.pos_z})"
            : $"[Key] note={key.key.note}, vel={key.velocity}";
    }

}

public class ClientRecieveEvent
{
    public User sender;
    public EventStruct evt;
    public static readonly int SIZE = EventStruct.EVENT_SIZE + User.USER_SIZE;

    public static ClientRecieveEvent UnPack(byte[] data)
    {
        if (data.Length < SIZE) throw new Exception("Not enough bytes for MoveEvent");
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var evt = new EventStruct();
        var sender = reader.ReadUInt32();

        evt.eventkind = (EventKind)reader.ReadByte();

        switch (evt.eventkind)
        {
            case EventKind.Join:
                break;
            case EventKind.DisConnect:
                break;
            case EventKind.Move:
                evt.move = MoveEvent.ReadByte(reader);
                break;
            case EventKind.Key:
                evt.key = KeyEvent.ReadByte(reader);
                break;
            default:
                throw new Exception($"Unknown event {data}");

        }

        var recieve = new ClientRecieveEvent();
        recieve.sender = new User(sender);
        recieve.evt = evt;
        return recieve;
    }
}

public class ClientSendEvent
{
    public static byte[] Pack(EventStruct evt)
    {
        byte[] data = new byte[EventStruct.EVENT_SIZE];
        using var ms = new MemoryStream(data);
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)evt.eventkind);

        if (evt.eventkind == EventKind.Move)
        {
            evt.move.WriteByte(writer);
        }
        else if (evt.eventkind == EventKind.Key)
        {
            evt.key.WriteByte(writer);
        }
        else
        {
            throw new Exception($"Unknown event {evt}");
        }

        return data;
    }
}
