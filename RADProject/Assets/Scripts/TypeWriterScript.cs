using System.Collections;
using TMPro;
using UnityEngine;

public class TypeWriterScript : MonoBehaviour
{
    public bool DoneTyping;
    public bool StartedTyping;
    
    public IEnumerator StartTyping(string fullText, float typingDelay = 0.05f)
    {
        DoneTyping = false;
        StartedTyping = true;
        GetComponent<TextMeshProUGUI>().text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            GetComponent<TextMeshProUGUI>().text = fullText.Substring(0, i);

            yield return new WaitForSeconds(typingDelay);
        }

        yield return new WaitForSeconds(typingDelay * fullText.Length);
        DoneTyping = true;
        StartedTyping = false;
    }
}
