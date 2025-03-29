using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private PlayerManager _playerManager;
    private PianoManager _pianoManager;

    private string _defaultHost = "127.0.0.1";
    private int _defaultPort = 8000;
    private TCPHelper _helper;
    private readonly Queue<ClientRecieveEvent> _eventQueue = new();
    private readonly object _queueLock = new();
    private int connetionFailCount = 0;

    private User my_id;

    bool isPaused = false;

    private (string host, int port) LoadOrCreateConfig()
    {
        string configPath;

#if UNITY_EDITOR
        configPath = Path.Combine(Application.dataPath, "../network_config.txt");
#else
    configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "network_config.txt");
#endif

        Logger.Log($"network_config.txt を探します: {configPath}");

        string host = _defaultHost;
        int port = _defaultPort;

        if (!File.Exists(configPath))
        {
            File.WriteAllLines(configPath, new string[]
            {
            $"host={_defaultHost}",
            $"port={_defaultPort}"
            });

            Logger.Log("network_config.txt がなかったため、 network_config.txt を生成しました。");
        }

        try
        {
            foreach (var line in File.ReadAllLines(configPath))
            {
                var tokens = line.Split('=', 2);
                if (tokens.Length != 2) continue;

                string key = tokens[0].Trim();
                string value = tokens[1].Trim();

                if (key == "host") host = value;
                else if (key == "port" && int.TryParse(value, out int p)) port = p;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"network_config.txt の読み込みに失敗しました。デフォルト値で続行します。\n{ex}");
            Logger.LogWarning("network_config.txt を確認してください。書式: host=xxx.xxx.xxx.xxx<改行>port=1234");
            host = _defaultHost;
            port = _defaultPort;
        }

        return (host, port);
    }

    public async void Start()
    {
        _playerManager = FindAnyObjectByType<PlayerManager>();
        _pianoManager = FindAnyObjectByType<PianoManager>();

        var (host, port) = LoadOrCreateConfig();

        Logger.Log($"接続先 {host}:{port}");

        _helper = new TCPHelper(host, port);
        while (!await _helper.Connect() && connetionFailCount < 5)
        {
            connetionFailCount++;
            await Task.Delay(1000);
        }

        // つながらなかった...
        if (connetionFailCount >= 5)
        {
            Logger.Log("接続失敗");
            return;
        }

        // つながった！
        Logger.Log("接続完了");

        // stream の用意もしないといけないから
        // こっちはつながったら成功するはず？
        while (!_helper.IsReady())
        {
            await Task.Delay(10);
        }

        var usr_id = await _helper.ReadExactAsync(User.USER_SIZE);
        my_id = new User(BitConverter.ToUInt32(usr_id, 0));

        Logger.Log($"あなたのID: {my_id.user}");

        _ = Task.Run(ReceiveLoop);
    }

    public async void Send(EventStruct evt)
    {

        // Start が完了する前に呼ばれるかも
        // => もみ消されるけどしょうがない（リアルタイムなので）
        if (_helper == null || !_helper.IsReady()) return;
        // isPaused の場合は送らない。
        if (isPaused) return;

        var data = ClientSendEvent.Pack(evt);
        await _helper.WriteExactAsync(data);
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (true)
            {
                byte[] data = await _helper.ReadExactAsync(ClientRecieveEvent.SIZE);
                var evt = ClientRecieveEvent.UnPack(data);

                lock (_queueLock)
                {
                    _eventQueue.Enqueue(evt);
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning($"ReceiveLoop error {e.Message}");
        }
    }

    public void Update()
    {
        lock (_queueLock)
        {
            while (_eventQueue.Count > 0)
            {
                ClientRecieveEvent evt = _eventQueue.Dequeue();
                switch (evt.evt.eventkind)
                {
                    case EventKind.Join: // pause 中も動かす
                        if (evt.sender != my_id)
                            _playerManager.OnJoinEvent(evt.sender);
                        break;
                    case EventKind.DisConnect: // pause 中も動かす
                        if (evt.sender != my_id)
                            _playerManager.OnDisconnectEvent(evt.sender);
                        break;
                    case EventKind.Move: // pause 中は動かさない
                        if (isPaused) break;
                        if (evt.sender != my_id)
                            _playerManager.OnMoveEvent(evt.evt.move, evt.sender);
                        break;
                    case EventKind.Key: // pause 中は動かさない
                        if (isPaused) break;
                        _pianoManager.OnKeyEvent(evt.evt.key, evt.sender);
                        break;
                    default:
                        Logger.LogWarning($"Unknown event kind: {evt.evt.eventkind}");
                        break;
                }
            }
        }
    }

    public void OnDestroy()
    {
        _helper?.Dispose();
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

class TCPHelper : IDisposable
{
    private string _host;
    private int _port;
    private TcpClient _client;
    private NetworkStream _stream;

    public TCPHelper(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public bool IsReady()
    {
        return (_client != null) && _client.Connected && (_stream != null);
    }

    public async Task<bool> Connect()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port);
            _stream = _client.GetStream();
            return true;
        }
        catch (SocketException ex)
        {
            Logger.Log($"connection failed {ex}");
            return false;
        }
    }

    public async Task<byte[]> ReadExactAsync(int totalBytes)
    {
        if (!IsReady())
            throw new InvalidOperationException("Not connected or disconnected");

        byte[] buffer = new byte[totalBytes];

        int total = 0;
        while (total < buffer.Length)
        {
            int read = await _stream.ReadAsync(buffer, total, totalBytes - total);
            if (read == 0)
                throw new EndOfStreamException("Connection closed before buffer was filled");
            total += read;
        }

        return buffer;
    }

    public async Task WriteExactAsync(byte[] wr)
    {
        if (!IsReady())
            throw new InvalidOperationException("Not connected or disconnected");

        await _stream.WriteAsync(wr);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _client?.Dispose();
    }
}