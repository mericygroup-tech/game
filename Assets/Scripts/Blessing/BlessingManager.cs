using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BlessingManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<BlessingDefinition> allBlessings = new List<BlessingDefinition>();

    [Header("Runtime")]
    [SerializeField] private BlessingRuntimeController playerEffects;

    [Header("Choice UI")]
    [SerializeField] private GameObject choiceRoot;
    [SerializeField] private BlessingChoiceUI[] choiceCards;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Image heroBackdrop;
    [SerializeField] private Sprite anDuongVuongBackdrop;
    [SerializeField] private Sprite trungTracBackdrop;
    [SerializeField] private Sprite trungNhiBackdrop;
    [SerializeField] private Sprite quangTrungBackdrop;
    [SerializeField] private TMP_Text heroNameText;
    [SerializeField] private TMP_Text heroLoreText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollButtonText;
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text skipButtonText;
    [SerializeField, Min(0)] private int rerollsPerChoice = 1;

    private readonly Dictionary<string, int> ownedStacks = new Dictionary<string, int>();
    private readonly List<BlessingDefinition> currentChoices = new List<BlessingDefinition>(3);
    private Action selectionComplete;
    private bool selectionOpen;
    private int rerollsRemaining;
    private HeroType previewHero = HeroType.AnDuongVuong;

    public bool IsSelectionOpen => selectionOpen;

    private void Awake()
    {
        ownedStacks.Clear();
        foreach (BlessingDefinition blessing in allBlessings)
        {
            if (blessing != null)
                blessing.SetRuntimeStack(0);
        }

        BindUtilityButtons();

        if (choiceRoot != null)
            choiceRoot.SetActive(false);
    }

    public void Configure(
        List<BlessingDefinition> definitions,
        BlessingRuntimeController effects,
        GameObject root,
        BlessingChoiceUI[] cards,
        TMP_Text title,
        TMP_Text result)
    {
        Configure(definitions, effects, root, cards, title, null, result, null, null, null, null, null, null, null, null, null, null, null);
    }

    public void Configure(
        List<BlessingDefinition> definitions,
        BlessingRuntimeController effects,
        GameObject root,
        BlessingChoiceUI[] cards,
        TMP_Text title,
        TMP_Text subtitle,
        TMP_Text result,
        Image backdrop,
        TMP_Text heroName,
        TMP_Text heroLore,
        Button reroll,
        TMP_Text rerollLabel,
        Button skip,
        TMP_Text skipLabel,
        Sprite anDuongVuongBackground = null,
        Sprite trungTracBackground = null,
        Sprite trungNhiBackground = null,
        Sprite quangTrungBackground = null)
    {
        allBlessings = definitions ?? new List<BlessingDefinition>();
        playerEffects = effects;
        choiceRoot = root;
        choiceCards = cards;
        titleText = title;
        subtitleText = subtitle;
        resultText = result;
        heroBackdrop = backdrop;
        heroNameText = heroName;
        heroLoreText = heroLore;
        rerollButton = reroll;
        rerollButtonText = rerollLabel;
        skipButton = skip;
        skipButtonText = skipLabel;
        if (anDuongVuongBackground != null)
            anDuongVuongBackdrop = anDuongVuongBackground;
        if (trungTracBackground != null)
            trungTracBackdrop = trungTracBackground;
        if (trungNhiBackground != null)
            trungNhiBackdrop = trungNhiBackground;
        if (quangTrungBackground != null)
            quangTrungBackdrop = quangTrungBackground;
        BindUtilityButtons();
    }

    public void PresentChoices(Action onComplete)
    {
        if (selectionOpen)
            return;

        selectionComplete = onComplete;
        rerollsRemaining = rerollsPerChoice;
        currentChoices.Clear();
        currentChoices.AddRange(RollDistinctChoices(3));
        if (currentChoices.Count == 0)
        {
            FinishSelection();
            return;
        }

        selectionOpen = true;
        playerEffects?.SetChoiceMode(true);

        if (choiceRoot != null)
            choiceRoot.SetActive(true);
        if (titleText != null)
            titleText.text = "CHỌN CHÚC PHÚC ANH LINH";
        if (resultText != null)
            resultText.text = "Chọn 1 trong 3 sức mạnh để định hình lối chơi.";

        ApplyChoicePromptText();
        BindCurrentChoices();
        PreviewBlessing(currentChoices[0]);
        UpdateUtilityButtons();
    }

    public int GetStack(string blessingId)
    {
        if (string.IsNullOrWhiteSpace(blessingId))
            return 0;
        return ownedStacks.TryGetValue(blessingId, out int stack) ? stack : 0;
    }

    public int GetEffectStack(BlessingEffectType effectType)
    {
        int total = 0;
        foreach (BlessingDefinition blessing in allBlessings)
        {
            if (blessing != null && blessing.EffectType == effectType)
                total += GetStack(blessing.Id);
        }
        return total;
    }

    public bool HasBlessing(BlessingEffectType effectType)
    {
        return GetEffectStack(effectType) > 0;
    }

    private List<BlessingDefinition> RollDistinctChoices(int count)
    {
        List<BlessingDefinition> pool = allBlessings
            .Where(blessing => blessing != null && !blessing.IsUltimate && GetStack(blessing.Id) < blessing.MaxStack)
            .GroupBy(blessing => blessing.Id)
            .Select(group => group.First())
            .ToList();

        List<BlessingDefinition> results = new List<BlessingDefinition>(count);
        while (pool.Count > 0 && results.Count < count)
        {
            int pickedIndex = PickWeightedIndex(pool);
            results.Add(pool[pickedIndex]);
            pool.RemoveAt(pickedIndex);
        }

        return results;
    }

    private static int PickWeightedIndex(IReadOnlyList<BlessingDefinition> pool)
    {
        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += GetRarityWeight(pool[i].Rarity);

        float roll = UnityEngine.Random.value * totalWeight;
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= GetRarityWeight(pool[i].Rarity);
            if (roll <= 0f)
                return i;
        }

        return pool.Count - 1;
    }

    private static float GetRarityWeight(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common: return 1f;
            case BlessingRarity.Rare: return 0.72f;
            case BlessingRarity.Epic: return 0.42f;
            case BlessingRarity.Legendary: return 0.18f;
            default: return 1f;
        }
    }

    private void BindCurrentChoices()
    {
        if (choiceCards == null || choiceCards.Length == 0)
        {
            if (currentChoices.Count > 0)
                SelectBlessing(currentChoices[0]);
            return;
        }

        for (int i = 0; i < choiceCards.Length; i++)
        {
            BlessingChoiceUI card = choiceCards[i];
            if (card == null)
                continue;

            BlessingDefinition blessing = i < currentChoices.Count ? currentChoices[i] : null;
            card.SetPreviewCallbacks(PreviewBlessing, RestoreDefaultPreview);
            card.Bind(blessing, blessing != null ? GetStack(blessing.Id) : 0, SelectBlessing);
        }
    }

    private void RerollChoices()
    {
        if (!selectionOpen || rerollsRemaining <= 0)
            return;

        rerollsRemaining--;
        currentChoices.Clear();
        currentChoices.AddRange(RollDistinctChoices(3));
        BindCurrentChoices();

        if (currentChoices.Count > 0)
            PreviewBlessing(currentChoices[0]);
        else
            SkipBlessing();

        UpdateUtilityButtons();
    }

    private void SkipBlessing()
    {
        if (!selectionOpen)
            return;

        selectionOpen = false;
        SetCardsInteractable(false);

        if (resultText != null)
            resultText.text = "B\u1ecf qua Blessing. Wave ti\u1ebfp theo b\u1eaft \u0111\u1ea7u.";

        UpdateUtilityButtons();
        StartCoroutine(FinishAfterFeedback());
    }

    private void SelectBlessing(BlessingDefinition blessing)
    {
        if (!selectionOpen || blessing == null)
            return;

        selectionOpen = false;
        SetCardsInteractable(false);
        PreviewBlessing(blessing);

        int newStack = Mathf.Min(GetStack(blessing.Id) + 1, blessing.MaxStack);
        ownedStacks[blessing.Id] = newStack;
        blessing.SetRuntimeStack(newStack);
        playerEffects?.ApplyBlessing(blessing, newStack);

        BlessingDefinition ultimate = TryUnlockUltimate(blessing.HeroType);
        string message = "Đã nhận: " + blessing.Name + "  [" + newStack + "/" + blessing.MaxStack + "]";
        if (ultimate != null)
            message += "\nMỞ KHÓA TỐI THƯỢNG: " + ultimate.Name;

        if (resultText != null)
            resultText.text = message;

        ApplySelectionMessage(blessing, ultimate, newStack);
        UpdateUtilityButtons();
        StartCoroutine(FinishAfterFeedback());
    }

    private BlessingDefinition TryUnlockUltimate(HeroType hero)
    {
        int distinctNormalBlessings = allBlessings.Count(blessing =>
            blessing != null &&
            blessing.HeroType == hero &&
            !blessing.IsUltimate &&
            GetStack(blessing.Id) > 0);

        if (distinctNormalBlessings < 3)
            return null;

        BlessingDefinition ultimate = allBlessings.FirstOrDefault(blessing =>
            blessing != null && blessing.HeroType == hero && blessing.IsUltimate);
        if (ultimate == null || GetStack(ultimate.Id) > 0)
            return null;

        ownedStacks[ultimate.Id] = 1;
        ultimate.SetRuntimeStack(1);
        playerEffects?.ApplyBlessing(ultimate, 1);
        return ultimate;
    }

    private IEnumerator FinishAfterFeedback()
    {
        yield return new WaitForSecondsRealtime(0.65f);
        FinishSelection();
    }

    private void FinishSelection()
    {
        selectionOpen = false;
        if (choiceRoot != null)
            choiceRoot.SetActive(false);
        playerEffects?.SetChoiceMode(false);

        Action callback = selectionComplete;
        selectionComplete = null;
        callback?.Invoke();
    }

    private void SetCardsInteractable(bool interactable)
    {
        if (choiceCards != null)
        {
            for (int i = 0; i < choiceCards.Length; i++)
            {
                if (choiceCards[i] != null)
                    choiceCards[i].SetInteractable(interactable);
            }
        }

        if (rerollButton != null)
            rerollButton.interactable = interactable && rerollsRemaining > 0;
        if (skipButton != null)
            skipButton.interactable = interactable;
    }

    private void ApplyChoicePromptText()
    {
        if (titleText != null)
            titleText.text = "CH\u1eccN BLESSING";
        if (subtitleText != null)
            subtitleText.text = "Ch\u1ecdn m\u1ed9t s\u1ee9c m\u1ea1nh \u0111\u1ec3 ti\u1ebfp t\u1ee5c h\u00e0nh tr\u00ecnh";
        if (resultText != null)
            resultText.text = "Di chu\u1ed9t qua card \u0111\u1ec3 xem chi ti\u1ebft, ho\u1eb7c ch\u1ecdn 1 Blessing.";

        RestoreDefaultPreview();
        UpdateUtilityButtons();
    }

    private void ApplySelectionMessage(BlessingDefinition blessing, BlessingDefinition ultimate, int newStack)
    {
        if (resultText == null || blessing == null)
            return;

        string safeMessage = "\u0110\u00e3 nh\u1eadn: " + blessing.Name + "  [" + newStack + "/" + blessing.MaxStack + "]";
        if (ultimate != null)
            safeMessage += "\nM\u1ede KH\u00d3A T\u1ed0I TH\u01af\u1ee2NG: " + ultimate.Name;

        resultText.text = safeMessage;
    }

    private void BindUtilityButtons()
    {
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(RerollChoices);
            rerollButton.onClick.AddListener(RerollChoices);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipBlessing);
            skipButton.onClick.AddListener(SkipBlessing);
        }
    }

    private void UpdateUtilityButtons()
    {
        if (rerollButton != null)
            rerollButton.interactable = selectionOpen && rerollsRemaining > 0;
        if (rerollButtonText != null)
            rerollButtonText.text = rerollsRemaining > 0 ? "L\u00c0M M\u1edaI (" + rerollsRemaining + ")" : "\u0110\u00c3 L\u00c0M M\u1edaI";
        if (skipButton != null)
            skipButton.interactable = selectionOpen;
        if (skipButtonText != null)
            skipButtonText.text = "B\u1ece QUA";
    }

    private void PreviewBlessing(BlessingDefinition blessing)
    {
        if (blessing == null)
            return;

        previewHero = blessing.HeroType;
        ApplyHeroBackdrop(previewHero);

        if (resultText != null)
        {
            int nextStack = Mathf.Min(GetStack(blessing.Id) + 1, blessing.MaxStack);
            resultText.text =
                blessing.Name +
                "  \u2022  " + GetHeroName(blessing.HeroType) +
                "  \u2022  " + GetRarityName(blessing.Rarity) +
                "\n" + blessing.Description +
                "\nB\u1eadc sau khi ch\u1ecdn: " + nextStack + "/" + blessing.MaxStack;
        }
    }

    private void RestoreDefaultPreview()
    {
        if (currentChoices.Count > 0 && currentChoices[0] != null)
            ApplyHeroBackdrop(currentChoices[0].HeroType);
        else
            ApplyHeroBackdrop(previewHero);
    }

    private void ApplyHeroBackdrop(HeroType hero)
    {
        Color heroColor = GetHeroColor(hero);
        if (heroBackdrop != null)
        {
            Sprite backdrop = GetHeroBackdrop(hero);
            if (backdrop != null)
            {
                heroBackdrop.sprite = backdrop;
                heroBackdrop.type = Image.Type.Simple;
                heroBackdrop.preserveAspect = false;
                heroBackdrop.color = new Color(1f, 1f, 1f, 0.96f);
            }
            else
            {
                heroBackdrop.sprite = null;
                heroBackdrop.color = new Color(heroColor.r, heroColor.g, heroColor.b, 0.78f);
            }
        }

        if (heroNameText != null)
            heroNameText.text = GetHeroName(hero);
        if (heroLoreText != null)
            heroLoreText.text = GetHeroLore(hero);
    }

    private Sprite GetHeroBackdrop(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return anDuongVuongBackdrop;
            case HeroType.TrungTrac: return trungTracBackdrop;
            case HeroType.TrungNhi: return trungNhiBackdrop;
            case HeroType.QuangTrung: return quangTrungBackdrop;
            default: return null;
        }
    }

    private static Color GetHeroColor(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return new Color(0.02f, 0.44f, 0.5f, 1f);
            case HeroType.TrungTrac: return new Color(0.56f, 0.07f, 0.16f, 1f);
            case HeroType.TrungNhi: return new Color(0.12f, 0.46f, 0.34f, 1f);
            case HeroType.QuangTrung: return new Color(0.78f, 0.34f, 0.05f, 1f);
            default: return new Color(0.1f, 0.08f, 0.06f, 1f);
        }
    }

    private static string GetHeroName(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return "AN D\u01af\u01a0NG V\u01af\u01a0NG";
            case HeroType.TrungTrac: return "TR\u01afNG TR\u1eaeC";
            case HeroType.TrungNhi: return "TR\u01afNG NH\u1eca";
            case HeroType.QuangTrung: return "QUANG TRUNG";
            default: return hero.ToString().ToUpperInvariant();
        }
    }

    private static string GetHeroLore(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong:
                return "N\u1ec1n th\u00e0nh C\u1ed5 Loa, r\u1ed3ng v\u00e0ng, tr\u1eddi chi\u1ec1u u nghi, m\u00e0u v\u00e0ng \u0111\u1ed3ng.";
            case HeroType.TrungTrac:
                return "N\u1ec1n c\u1edd kh\u1edfi ngh\u0129a, voi chi\u1ebfn, tr\u1eddi \u0111\u1ecf h\u00f9ng tr\u00e1ng.";
            case HeroType.TrungNhi:
                return "N\u1ec1n hoa sen, s\u00f4ng n\u01b0\u1edbc, tr\u1eddi t\u00edm h\u1ed3ng, m\u1ec1m m\u1ea1i nh\u01b0ng m\u1ea1nh m\u1ebd.";
            case HeroType.QuangTrung:
                return "N\u1ec1n \u0111\u1ea1i qu\u00e2n T\u00e2y S\u01a1n, n\u00fai r\u1eebng, l\u1eeda chi\u1ebfn, kh\u00ed th\u1ebf th\u1ea7n t\u1ed1c.";
            default:
                return string.Empty;
        }
    }

    private static string GetRarityName(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common: return "COMMON";
            case BlessingRarity.Rare: return "RARE";
            case BlessingRarity.Epic: return "EPIC";
            case BlessingRarity.Legendary: return "LEGENDARY";
            default: return rarity.ToString().ToUpperInvariant();
        }
    }
}
