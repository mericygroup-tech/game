using UnityEngine;

[CreateAssetMenu(fileName = "Blessing", menuName = "Dong Chay Anh Hung/Blessing")]
public sealed class BlessingDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private HeroType heroType;
    [SerializeField] private Sprite icon;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private BlessingRarity rarity;
    [Min(1)]
    [SerializeField] private int maxStack = 3;
    [SerializeField] private BlessingEffectType effectType;
    [SerializeField] private bool ultimate;

    [System.NonSerialized] private int currentStack;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string Name => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
    public HeroType HeroType => heroType;
    public Sprite Icon => icon;
    public string Description => description;
    public BlessingRarity Rarity => rarity;
    public int MaxStack => Mathf.Max(1, maxStack);
    public int CurrentStack => currentStack;
    public BlessingEffectType EffectType => effectType;
    public bool IsUltimate => ultimate;

    public void SetRuntimeStack(int value)
    {
        currentStack = Mathf.Clamp(value, 0, MaxStack);
    }

#if UNITY_EDITOR
    public void Configure(
        string blessingId,
        string blessingName,
        HeroType hero,
        string blessingDescription,
        BlessingRarity blessingRarity,
        int stackLimit,
        BlessingEffectType effect,
        bool isUltimate)
    {
        id = blessingId;
        displayName = blessingName;
        heroType = hero;
        description = blessingDescription;
        rarity = blessingRarity;
        maxStack = Mathf.Max(1, stackLimit);
        effectType = effect;
        ultimate = isUltimate;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
