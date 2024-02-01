using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class TotalTimeScript : MonoBehaviour
{
    [SerializeField]
    private TMP_Text aiTextForFirstWord;

    [SerializeField]
    private TMP_Text aiTextForLastWord;

    [SerializeField]
    private TMP_Text aiTextFirstSentence;

    [SerializeField]
    private TMP_Text tts;

    [SerializeField]
    private TMP_Text total;

    private string oldTextFirstSentence;
    private string oldTextTts;
    // Start is called before the first frame update
    void Start()
    {
        oldTextFirstSentence = aiTextFirstSentence.text;
        oldTextTts = tts.text;
    }
    // Update is called once per frame
    void Update()
    {
        if (aiTextFirstSentence.text != oldTextFirstSentence && tts.text != oldTextTts)
        {
            String aiText = Regex.Match(aiTextFirstSentence.text, @"\d+").Value;
            String ttsText = Regex.Match(tts.text, @"\d+").Value;
            if (aiText.Length == 0) aiText = "0";
            if (ttsText.Length == 0) ttsText = "0";
            int aiNumber = Int32.Parse(aiText);
            int ttsNumber = Int32.Parse(ttsText);
            int totalTime = aiNumber + ttsNumber;
            string totalLog = "Total: " + totalTime + "ms";
            Debug.Log(totalLog);
            total.text = totalLog;
            oldTextTts = tts.text;
            oldTextFirstSentence = aiTextFirstSentence.text;
        }
    }
}
