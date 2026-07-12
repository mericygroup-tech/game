using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BlessingChoiceUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image frame;
    [SerializeField] private Image cardBody;
    [SerializeField] private Image glow;
    [SerializeField] private Image icon;
    [SerializeField] private Image rarityGem;
    [SerializeField] private TMP_Text heroText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text stackText;

    private BlessingDefinition definition;
    private Action<BlessingDefinition> onSelected;
    private Action<BlessingDefinition> onPreviewRequested;
    private Action onPreviewCleared;
    private Vector3 baseScale = Vector3.one;
    private bool highlighted;

    public void ConfigureReferences(
        Button choiceButton,
        Image choiceFrame,
        Image choiceIcon,
        TMP_Text choiceHero,
        TMP_Text choiceName,
        TMP_Text choiceDescription,
        TMP_Text choiceRarity,
        TMP_Text choiceStack)
    {
        ConfigureReferences(
            choiceButton,
            choiceFrame,
            null,
            null,
            choiceIcon,
            null,
            choiceHero,
            choiceName,
            choiceDescription,
            choiceRarity,
            choiceStack);
    }

    public void ConfigureReferences(
        Button choiceButton,
        Image choiceFrame,
        Image choiceBody,
        Image choiceGlow,
        Image choiceIcon,
        Image choiceRarityGem,
        TMP_Text choiceHero,
        TMP_Text choiceName,
        TMP_Text choiceDescription,
        TMP_Text choiceRarity,
        TMP_Text choiceStack)
    {
        button = choiceButton;
        frame = choiceFrame;
        cardBody = choiceBody;
        glow = choiceGlow;
        icon = choiceIcon;
        rarityGem = choiceRarityGem;
        heroText = choiceHero;
        nameText = choiceName;
        descriptionText = choiceDescription;
        rarityText = choiceRarity;
        stackText = choiceStack;
    }

    public void SetPreviewCallbacks(Action<BlessingDefinition> previewCallback, Action clearCallback)
    {
        onPreviewRequested = previewCallback;
        onPreviewCleared = clearCallback;
    }

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        Vector3 targetScale = highlighted && definition != null ? baseScale * 1.045f : baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 12f);

        if (glow != null)
        {
            Color color = glow.color;
            float targetAlpha = highlighted && definition != null ? 0.42f : 0.08f;
            color.a = Mathf.Lerp(color.a, targetAlpha, Time.unscaledDeltaTime * 12f);
            glow.color = color;
        }
    }

    public void Bind(BlessingDefinition blessing, int ownedStack, Action<BlessingDefinition> selectionCallback)
    {
        definition = blessing;
        onSelected = selectionCallback;
        highlighted = false;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Select);
            button.interactable = blessing != null;
            button.transition = Selectable.Transition.None;
        }

        if (blessing == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        Color heroColor = GetHeroColor(blessing.HeroType);
        Color rarityColor = GetRarityColor(blessing.Rarity);

        if (frame != null)
            frame.color = Color.Lerp(new Color(0.06f, 0.045f, 0.035f, 0.98f), rarityColor, 0.35f);

        if (cardBody != null)
            cardBody.color = Color.Lerp(new Color(0.025f, 0.026f, 0.03f, 0.94f), heroColor, 0.16f);

        if (glow != null)
        {
            glow.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.08f);
        }

        if (rarityGem != null)
            rarityGem.color = rarityColor;

        if (icon != null)
        {
            icon.sprite = blessing.Icon;
            icon.color = blessing.Icon != null ? Color.white : heroColor;
        }

        if (heroText != null)
            heroText.text = GetHeroName(blessing.HeroType);
        if (nameText != null)
            nameText.text = blessing.Name;
        if (descriptionText != null)
            descriptionText.text = blessing.Description;
        if (rarityText != null)
        {
            rarityText.text = GetRarityName(blessing.Rarity);
            rarityText.color = rarityColor;
        }

        if (stackText != null)
        {
            int nextStack = Mathf.Min(ownedStack + 1, blessing.MaxStack);
            stackText.text = blessing.MaxStack > 1
                ? "Bậc " + nextStack + "/" + blessing.MaxStack
                : (blessing.IsUltimate ? "Tối thượng" : "Độc nhất");
        }

        ApplySafeDisplayText(blessing, ownedStack);
    }

    private void Select()
    {
        if (definition == null)
            return;

        onPreviewRequested?.Invoke(definition);
        if (button != null)
            button.interactable = false;
        onSelected?.Invoke(definition);
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable && definition != null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Preview();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        highlighted = false;
        onPreviewCleared?.Invoke();
    }

    public void OnSelect(BaseEventData eventData)
    {
        Preview();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        highlighted = false;
    }

    private void Preview()
    {
        if (definition == null)
            return;

        highlighted = true;
        onPreviewRequested?.Invoke(definition);
    }

    private void ApplySafeDisplayText(BlessingDefinition blessing, int ownedStack)
    {
        if (blessing == null)
            return;

        if (heroText != null)
            heroText.text = GetSafeHeroName(blessing.HeroType);
        if (nameText != null)
            nameText.text = blessing.Name;
        if (descriptionText != null)
            descriptionText.text = blessing.Description;
        if (rarityText != null)
            rarityText.text = GetSafeRarityName(blessing.Rarity);
        if (stackText != null)
        {
            int nextStack = Mathf.Min(ownedStack + 1, blessing.MaxStack);
            stackText.text = blessing.MaxStack > 1
                ? "B\u1eadc " + nextStack + "/" + blessing.MaxStack
                : (blessing.IsUltimate ? "T\u1ed1i th\u01b0\u1ee3ng" : "\u0110\u1ed9c nh\u1ea5t");
        }
    }

    private static string GetSafeHeroName(HeroType hero)
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

    private static string GetSafeRarityName(BlessingRarity rarity)
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

    public static string GetHeroName(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return "AN DƯƠNG VƯƠNG";
            case HeroType.TrungTrac: return "TRƯNG TRẮC";
            case HeroType.TrungNhi: return "TRƯNG NHỊ";
            case HeroType.QuangTrung: return "QUANG TRUNG";
            default: return hero.ToString().ToUpperInvariant();
        }
    }

    public static Color GetHeroColor(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return new Color(0.12f, 0.68f, 0.88f, 1f);
            case HeroType.TrungTrac: return new Color(0.82f, 0.18f, 0.32f, 1f);
            case HeroType.TrungNhi: return new Color(0.25f, 0.82f, 0.58f, 1f);
            case HeroType.QuangTrung: return new Color(0.96f, 0.62f, 0.12f, 1f);
            default: return Color.white;
        }
    }

    public static Color GetRarityColor(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common: return new Color(0.72f, 0.76f, 0.8f, 1f);
            case BlessingRarity.Rare: return new Color(0.18f, 0.58f, 1f, 1f);
            case BlessingRarity.Epic: return new Color(0.68f, 0.24f, 0.96f, 1f);
            case BlessingRarity.Legendary: return new Color(1f, 0.62f, 0.08f, 1f);
            default: return Color.white;
        }
    }

    private static string GetRarityName(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common: return "THƯỜNG";
            case BlessingRarity.Rare: return "HIẾM";
            case BlessingRarity.Epic: return "SỬ THI";
            case BlessingRarity.Legendary: return "HUYỀN THOẠI";
            default: return rarity.ToString().ToUpperInvariant();
        }
    }
}
