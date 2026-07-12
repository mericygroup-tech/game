using UnityEngine;
using UnityEngine.UI;

public static class AudioUiFeedbackInstaller
{
    public static void Install()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.GetComponent<AudioButtonFeedback>() == null)
                button.gameObject.AddComponent<AudioButtonFeedback>();
        }

        Slider[] sliders = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include);
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null || slider.GetComponent<AudioSliderFeedback>() != null)
                continue;

            string sliderName = slider.name.ToLowerInvariant();
            if (sliderName.Contains("volume") || sliderName.Contains("audio") || sliderName.Contains("sensitivity"))
                slider.gameObject.AddComponent<AudioSliderFeedback>();
        }
    }
}
