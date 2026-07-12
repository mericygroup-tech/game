using UnityEngine;
using System.Collections.Generic;

public class SlowZone : MonoBehaviour
{
    public float slowMoveSpeed = 1.5f;

    private readonly Dictionary<PlayerController3D, float> originalSpeeds = new Dictionary<PlayerController3D, float>();

    private void OnTriggerEnter(Collider other)
    {
        PlayerController3D player = GetPlayerController(other);

        if (player == null || originalSpeeds.ContainsKey(player))
            return;

        originalSpeeds.Add(player, player.moveSpeed);

        player.moveSpeed = slowMoveSpeed;

        Debug.Log("Entered SlowZone");
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController3D player = GetPlayerController(other);

        if (player == null || !originalSpeeds.TryGetValue(player, out float originalSpeed))
            return;

        player.moveSpeed = originalSpeed;
        originalSpeeds.Remove(player);

        Debug.Log("Exited SlowZone");
    }

    private PlayerController3D GetPlayerController(Collider other)
    {
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
            return null;

        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController3D>();

        return player;
    }
}
