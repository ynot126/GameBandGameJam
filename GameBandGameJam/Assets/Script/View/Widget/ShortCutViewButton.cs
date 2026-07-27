#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShortCutViewButton : MonoBehaviour
{
    [SerializeField] Button button = null!;
    [SerializeField] TextMeshProUGUI text = null!;
    public TextMeshProUGUI Text => text;
    public Button Button => button;
}
