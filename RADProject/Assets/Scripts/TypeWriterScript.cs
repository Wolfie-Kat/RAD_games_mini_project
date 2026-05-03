using System.Collections;
using TMPro;
using UnityEngine;

public class TypeWriterScript : MonoBehaviour
{
    public bool DoneTyping;
    public bool StartedTyping;
    
    private TextMeshProUGUI _textComponent;
    private string _fullText;
    
    void Awake()
    {
        _textComponent = GetComponent<TextMeshProUGUI>();
    }
    
    public IEnumerator StartTyping(string fullText, float typingDelay = 0.04f)
    {
        DoneTyping = false;
        StartedTyping = true;
        _fullText = fullText;
        
        // Set the full text but make it transparent using alpha
        _textComponent.text = fullText;
        _textComponent.ForceMeshUpdate(); // Force immediate layout calculation
        
        // Get the text info and make all characters invisible
        TMP_TextInfo textInfo = _textComponent.textInfo;
        int totalCharacters = textInfo.characterCount;
        
        // Create a color array for visible characters
        Color32[] newVertexColors;
        
        // Initially set all characters to invisible (alpha = 0)
        for (int i = 0; i < totalCharacters; i++)
        {
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            newVertexColors = textInfo.meshInfo[materialIndex].colors32;
            
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            
            // Set alpha to 0 for this character's 4 vertices
            for (int j = 0; j < 4; j++)
            {
                if (vertexIndex + j < newVertexColors.Length)
                {
                    newVertexColors[vertexIndex + j].a = 0;
                }
            }
        }
        
        // Update all meshes with invisible text
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            if (textInfo.meshInfo[i].mesh != null)
            {
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                _textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
        
        // Reveal characters one by one
        for (int i = 0; i < totalCharacters; i++)
        {
            // Make the i-th character visible
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            newVertexColors = textInfo.meshInfo[materialIndex].colors32;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            
            // Set alpha to 255 for this character's 4 vertices
            for (int j = 0; j < 4; j++)
            {
                if (vertexIndex + j < newVertexColors.Length)
                {
                    newVertexColors[vertexIndex + j].a = 255;
                }
            }
            
            // Update the mesh for this material
            textInfo.meshInfo[materialIndex].mesh.colors32 = newVertexColors;
            _textComponent.UpdateGeometry(textInfo.meshInfo[materialIndex].mesh, materialIndex);
            
            AudioManager.Instance.Play(SoundType.Text_Boop);
            yield return new WaitForSeconds(typingDelay);
        }

        yield return new WaitForSeconds(typingDelay * fullText.Length);
        
        DoneTyping = true;
        StartedTyping = false;
    }
}