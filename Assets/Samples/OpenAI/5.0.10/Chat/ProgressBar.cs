using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;

namespace OpenAI.Samples.Chat
{
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField]
        private Image fillImage;
        [SerializeField]
        private TMP_Text timerText;

        [SerializeField]
        private Button recordButton;
        [SerializeField]
        private Button stopRecordButton;

        void Start()
        {
            fillImage.fillAmount = 0.0f;
            fillImage.color = Color.green;
            initAll();
            recordButton.onClick.AddListener(() =>
            {
                FillProgressBarOverDuration(10f);
            });
            stopRecordButton.onClick.AddListener(() =>
            {
                initAll();
            });
        }
        public void FillProgressBarOverDuration(float seconds)
        {
            StartCoroutine(FillProgressBarCoroutine(seconds));
        }
        private void initAll()
        {
            StopAllCoroutines();
            fillImage.fillAmount = 0.0f;
            fillImage.color = Color.green;
            recordButton.SetActive(true);
            stopRecordButton.SetActive(false);
            timerText.text = "";
        }
        private IEnumerator FillProgressBarCoroutine(float seconds)
        {
            recordButton.SetActive(false);
            stopRecordButton.SetActive(true);
            float elapsedTime = 0f;
            float initialFillAmount = fillImage.fillAmount;
            float targetFillAmount = 1f;
            timerText.text = "Time Left: " + seconds;
            while (elapsedTime < seconds)
            {
                fillImage.fillAmount = Mathf.Lerp(initialFillAmount, targetFillAmount, elapsedTime / seconds);
                int timeleft = Mathf.FloorToInt(seconds - elapsedTime);
                timerText.text = "Time Left: " + timeleft;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            fillImage.fillAmount = targetFillAmount;
            initAll();
        }
    }
}
