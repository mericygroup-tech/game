using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class S01ChaseRouteRepairBuilder
{
    public static void RepairS01ChaseThreatWaypoints()
    {
        GameObject waypointRoot = GameObject.Find("S01_ChaseWaypoints");
        if (waypointRoot == null)
        {
            Debug.LogWarning("S01ChaseRouteRepairBuilder: S01_ChaseWaypoints not found.");
            return;
        }

        Transform[] route = new Transform[waypointRoot.transform.childCount];
        for (int i = 0; i < waypointRoot.transform.childCount; i++)
            route[i] = waypointRoot.transform.GetChild(i);

        Array.Sort(route, CompareWaypointNames);

        S01ChaseThreat[] threats = UnityEngine.Object.FindObjectsByType<S01ChaseThreat>(FindObjectsInactive.Include);
        foreach (S01ChaseThreat threat in threats)
        {
            if (threat == null)
                continue;

            threat.waypoints = route;
            threat.farPlayerDistance = Mathf.Max(threat.farPlayerDistance, 32f);
            threat.veryFarPlayerDistance = Mathf.Max(threat.veryFarPlayerDistance, 48f);
            EditorUtility.SetDirty(threat);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("S01ChaseRouteRepairBuilder: repaired " + threats.Length + " chase threat route(s) with " + route.Length + " waypoint(s).");
    }

    private static int CompareWaypointNames(Transform left, Transform right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.CompareOrdinal(leftName, rightName);
    }
}
