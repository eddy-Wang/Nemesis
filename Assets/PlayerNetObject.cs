using Mirror;
using UnityEngine;
public class PlayerNetObject : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnPlayerNumberChanged))]
    public int playerNumber = 0;
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log($"I'm a local player, my netId is: {netId}. I'm player number: {playerNumber}");
    }
    void OnPlayerNumberChanged(int oldNum, int newNum)
    {
        gameObject.name = $"PlayerNetObject_{newNum} (netId: {netId})";
    }
}
