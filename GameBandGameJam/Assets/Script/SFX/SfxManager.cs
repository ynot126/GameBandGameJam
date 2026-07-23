#nullable enable
using UnityEngine;

public class SfxManager : Singleton<SfxManager>
{
    [SerializeField] EnumDictionary<SfxType, AudioClip> sfxDict = new EnumDictionary<SfxType, AudioClip>();
    [SerializeField] AudioSource audioSource = null!;
    public override void Initialize()
    {
        
    }

    public void Play(SfxType sfxType)
    {
        if(sfxType!=SfxType.None)
            audioSource.PlayOneShot(sfxDict[sfxType]);
    }
}
