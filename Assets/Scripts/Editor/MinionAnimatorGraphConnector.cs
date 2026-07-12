using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MinionAnimatorGraphConnector
{
    private const string ControllerPath = "Assets/Animations/Minion/Minion.controller";
    private const string MoveSpeedParameter = "MoveSpeed";
    private const string AttackParameter = "Attack";
    private const string DieParameter = "Die";

    [MenuItem("Tools/Dong Chay Anh Hung/Reconnect Minion Animator Graph")]
    public static void ReconnectMinionAnimatorGraph()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("MinionAnimatorGraphConnector: controller not found at " + ControllerPath);
            return;
        }

        EnsureParameter(controller, MoveSpeedParameter, AnimatorControllerParameterType.Float);
        EnsureParameter(controller, AttackParameter, AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, DieParameter, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = FindState(stateMachine, "Idle");
        AnimatorState walk = FindState(stateMachine, "Walk");
        AnimatorState run = FindState(stateMachine, "Run");
        AnimatorState attack = FindState(stateMachine, "Attack");
        AnimatorState death = FindState(stateMachine, "Death");

        if (idle == null || walk == null || run == null || attack == null || death == null)
        {
            Debug.LogError("MinionAnimatorGraphConnector: missing one or more Minion states.");
            return;
        }

        ClearTransitions(stateMachine, idle, walk, run, attack, death);

        AddFloatTransition(idle, walk, AnimatorConditionMode.Greater, 0.05f, MoveSpeedParameter, false, 0.08f);
        AddFloatTransition(walk, idle, AnimatorConditionMode.Less, 0.05f, MoveSpeedParameter, false, 0.08f);
        AddFloatTransition(walk, run, AnimatorConditionMode.Greater, 0.65f, MoveSpeedParameter, false, 0.08f);
        AddFloatTransition(run, walk, AnimatorConditionMode.Less, 0.65f, MoveSpeedParameter, false, 0.08f);
        AddFloatTransition(run, idle, AnimatorConditionMode.Less, 0.05f, MoveSpeedParameter, false, 0.08f);
        AddTriggerTransition(stateMachine, attack, AttackParameter, 0.03f);
        AddTriggerTransition(stateMachine, death, DieParameter, 0.03f);
        AddExitTransition(attack, idle, 0.88f, 0.08f);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(stateMachine);
        AssignControllerToSceneMinions(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int stateTransitionCount = stateMachine.states.Sum(child => child.state.transitions.Length);
        Debug.Log("MinionAnimatorGraphConnector: connected Minion controller with " +
            stateTransitionCount + " state transition(s), " +
            stateMachine.anyStateTransitions.Length + " Any State transition(s), and " +
            controller.parameters.Length + " parameter(s).");
    }

    private static void AssignControllerToSceneMinions(RuntimeAnimatorController controller)
    {
        string[] scenePaths =
        {
            "Assets/Scenes/S01_CityPrototype.unity",
            "Assets/Scenes/S02_UndergroundCave.unity"
        };

        foreach (string scenePath in scenePaths)
        {
            if (!System.IO.File.Exists(scenePath))
                continue;

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool changed = false;

            S01ChaseThreat[] chaseThreats = Object.FindObjectsByType<S01ChaseThreat>(FindObjectsInactive.Include);
            foreach (S01ChaseThreat chaseThreat in chaseThreats)
                changed |= AssignControllerToAnimators(chaseThreat.GetComponentsInChildren<Animator>(true), controller);

            MinionChase3D[] minions = Object.FindObjectsByType<MinionChase3D>(FindObjectsInactive.Include);
            foreach (MinionChase3D minion in minions)
                changed |= AssignControllerToAnimators(minion.GetComponentsInChildren<Animator>(true), controller);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }

    private static bool AssignControllerToAnimators(Animator[] animators, RuntimeAnimatorController controller)
    {
        bool changed = false;
        foreach (Animator animator in animators)
        {
            if (animator == null || animator.runtimeAnimatorController == controller)
                continue;

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);
            changed = true;
        }

        return changed;
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        AnimatorControllerParameter existing = controller.parameters.FirstOrDefault(parameter => parameter.name == name);
        if (existing != null)
        {
            if (existing.type != type)
            {
                controller.RemoveParameter(existing);
                controller.AddParameter(name, type);
            }

            return;
        }

        controller.AddParameter(name, type);
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
    {
        return stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == name);
    }

    private static void ClearTransitions(AnimatorStateMachine stateMachine, params AnimatorState[] states)
    {
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
            stateMachine.RemoveAnyStateTransition(transition);

        foreach (AnimatorState state in states)
        {
            foreach (AnimatorStateTransition transition in state.transitions.ToArray())
                state.RemoveTransition(transition);
        }
    }

    private static AnimatorStateTransition AddFloatTransition(
        AnimatorState from,
        AnimatorState to,
        AnimatorConditionMode mode,
        float threshold,
        string parameter,
        bool hasExitTime,
        float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = hasExitTime ? 0.85f : 0f;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(mode, threshold, parameter);
        return transition;
    }

    private static AnimatorStateTransition AddTriggerTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState to,
        string parameter,
        float duration)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        return transition;
    }

    private static AnimatorStateTransition AddExitTransition(
        AnimatorState from,
        AnimatorState to,
        float exitTime,
        float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        return transition;
    }
}
