using UnityEngine;

public class MinionDeathNotifier : MonoBehaviour
{
    public MinionSpawner3D spawner;
    public bool notifyOnlyWhenKilled = true;
    private bool killed;

    public void MarkKilled()
    {
        killed = true;
    }

    private void OnDestroy()
    {
        if (notifyOnlyWhenKilled && !killed)
            return;

        if (spawner != null)
        {
            spawner.NotifyEnemyDied();
        }
    }
}
