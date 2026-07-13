#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

internal static class GameViewFitUtility
{
    private const string MenuPath = "Tools/Dong Chay Anh Hung/Game View/Fit To Window";
    private const BindingFlags InstanceFields =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [MenuItem(MenuPath)]
    public static void FitToWindow()
    {
        Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
        {
            Debug.LogWarning("GameViewFitUtility: UnityEditor.GameView was not found.");
            return;
        }

        FieldInfo zoomAreaField = FindField(gameViewType, "m_ZoomArea");
        if (zoomAreaField == null)
        {
            Debug.LogWarning("GameViewFitUtility: Game View zoom state was not found.");
            return;
        }

        UnityEngine.Object[] gameViews = Resources.FindObjectsOfTypeAll(gameViewType);
        int updatedCount = 0;

        foreach (UnityEngine.Object gameView in gameViews)
        {
            object zoomArea = zoomAreaField.GetValue(gameView);
            if (zoomArea == null || !SetField(zoomArea, "m_ScaleWithWindow", true))
                continue;

            // Reset any manual panning left behind by a zoomed Game View. Unity
            // recalculates the exact fitted scale during the next repaint.
            SetField(zoomArea, "m_Translation", Vector2.zero);
            zoomAreaField.SetValue(gameView, zoomArea);

            if (gameView is EditorWindow window)
                window.Repaint();

            updatedCount++;
        }

        EditorApplication.QueuePlayerLoopUpdate();
        Debug.Log($"GameViewFitUtility: fitted {updatedCount} Game View window(s).");
    }

    private static FieldInfo FindField(Type type, string fieldName)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            FieldInfo field = current.GetField(fieldName, InstanceFields);
            if (field != null)
                return field;
        }

        return null;
    }

    private static bool SetField(object target, string fieldName, object value)
    {
        FieldInfo field = FindField(target.GetType(), fieldName);
        if (field == null)
            return false;

        field.SetValue(target, value);
        return true;
    }
}
#endif
