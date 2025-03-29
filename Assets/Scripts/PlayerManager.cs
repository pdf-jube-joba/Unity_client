using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    private Dictionary<User, GameObject> _players = new();

    public void OnMoveEvent(MoveEvent evt, User sender)
    {
        if (_players.TryGetValue(sender, out var obj))
        {
            obj.transform.position = evt.Pos();
            obj.transform.rotation = evt.Ori();
        }
        else
        {
            // すでに入室している人のイベントと思われる。
            var playerObj = Instantiate(playerPrefab);
            playerObj.name = $"{sender}";
            _players[sender] = playerObj;
        }
    }
    public void OnJoinEvent(User sender)
    {
        if (_players.ContainsKey(sender))
        {
            // Logger.Log("join ?");
        }
        else
        {
            // Logger.Log("join");
            var playerObj = Instantiate(playerPrefab);
            playerObj.name = $"{sender}";
            _players[sender] = playerObj;

        }

    }
    public void OnDisconnectEvent(User sender)
    {
        if (_players.TryGetValue(sender, out var obj))
        {
            // Logger.Log("disconnect");
            Destroy(obj);
            _players.Remove(sender);
        }
        else
        {
            // 一切音沙汰のない人のイベントと思われる。
            // Logger.Log("disconnect ?");
        }
    }
}