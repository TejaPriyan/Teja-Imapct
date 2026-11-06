using UnityEngine;
using System;
using System.Collections.Generic;

// The original score and effects are generated offline by Assets/AudioSource/generate_audio.py.
// Runtime playback reuses cached clips and a bounded pool; it never synthesizes per attack.
public partial class TejaAdventure {
    float musicVolume=.38f,sfxVolume=.72f;
    float MusicVolume {get{return musicVolume;}}
    float SfxVolume {get{return sfxVolume;}}
    bool audioReady,audioLastEnabled,audioSettingsDirty,audioWasGrounded;
    float audioSaveDelay,audioCombatHold,audioFootstepClock;
    Vector3 audioLastPosition;
    Phase audioLastPhase;
    readonly Dictionary<string,AudioClip> audioEffects=new Dictionary<string,AudioClip>();
    readonly Dictionary<string,float> audioRecentEffects=new Dictionary<string,float>();
    readonly System.Random audioRandom=new System.Random(60831);
    AudioSource[] audioMusic,audioVoices;
    float[] audioMusicBlend,audioVoiceGains;
    int audioNextVoice;
    static readonly string[] AudioMusicNames={"music-exploration","music-combat","music-boss"};
    static readonly string[] AudioEffectNames={"sword1","sword2","sword3","sidearm","rifle","reload","reload_done","hit","boss_hit","hurt","coin","treasure","heal","level","victory","ui","jump","dodge","footstep","burst","warning"};

    void InitializeAudio(){
        if(audioReady)return;
        musicVolume=ValidAudioVolume(PlayerPrefs.GetFloat("TejaMusicVolume",.38f),.38f);
        sfxVolume=ValidAudioVolume(PlayerPrefs.GetFloat("TejaSfxVolume",.72f),.72f);
        soundOn=PlayerPrefs.GetInt("TejaAudioEnabled",1)!=0;audioLastEnabled=soundOn;
        audioMusic=new AudioSource[3];audioMusicBlend=new float[3];audioVoices=new AudioSource[10];audioVoiceGains=new float[10];
        for(int i=0;i<audioMusic.Length;i++){
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.spatialBlend=0;
            source.priority=120;source.volume=0;source.clip=Resources.Load<AudioClip>("Audio/"+AudioMusicNames[i]);audioMusic[i]=source;
            if(!source.clip)Debug.LogWarning("Missing score: "+AudioMusicNames[i]);
        }
        for(int i=0;i<audioVoices.Length;i++){
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
            source.priority=80;source.volume=0;audioVoices[i]=source;
        }
        foreach(string key in AudioEffectNames){var clip=Resources.Load<AudioClip>("Audio/"+key);if(clip)audioEffects[key]=clip;else Debug.LogWarning("Missing effect: "+key);}
        audioReady=true;audioLastPosition=hero?hero.position:Vector3.zero;audioLastPhase=phase;
        if(!smoke){double startsAt=AudioSettings.dspTime+.15;foreach(var source in audioMusic)if(source.clip)source.PlayScheduled(startsAt);}
    }
    static float ValidAudioVolume(float value,float fallback){return float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp01(value);}
    void SetMusicVolume(float value){value=ValidAudioVolume(value,musicVolume);if(Mathf.Abs(value-musicVolume)<.001f)return;musicVolume=value;MarkAudioSettingsDirty();}
    void SetSfxVolume(float value){value=ValidAudioVolume(value,sfxVolume);if(Mathf.Abs(value-sfxVolume)<.001f)return;sfxVolume=value;MarkAudioSettingsDirty();}
    void SetAudioEnabled(bool enabled){if(soundOn==enabled)return;soundOn=enabled;MarkAudioSettingsDirty();}
    void MarkAudioSettingsDirty(){audioSettingsDirty=true;audioSaveDelay=.5f;}
    void SaveAudioSettings(){
        if(!audioReady)return;
        if(smoke||inputCheck){audioSettingsDirty=false;return;}
        PlayerPrefs.SetFloat("TejaMusicVolume",musicVolume);PlayerPrefs.SetFloat("TejaSfxVolume",sfxVolume);
        PlayerPrefs.SetInt("TejaAudioEnabled",soundOn?1:0);PlayerPrefs.Save();audioSettingsDirty=false;
    }
    void TickAudio(float dt){
        if(!audioReady)return;
        if(soundOn!=audioLastEnabled){audioLastEnabled=soundOn;MarkAudioSettingsDirty();}
        if(audioSettingsDirty){audioSaveDelay-=dt;if(audioSaveDelay<=0)SaveAudioSettings();}
        if(smoke)return;
        bool activeFight=false,bossFight=false;
        if(phase==Phase.Play&&hero){
            bossFight=boss!=null&&boss.active&&!boss.dead&&FlatDistance(hero.position,boss.root.position)<45;
            foreach(var enemy in foes)if(enemy.active&&!enemy.dead&&FlatDistance(hero.position,enemy.root.position)<16){activeFight=true;break;}
            if(activeFight||bossFight)audioCombatHold=5;else audioCombatHold=Mathf.Max(0,audioCombatHold-dt);
        }
        int target=phase==Phase.Play?(bossFight?2:audioCombatHold>0?1:0):0;
        float quiet=phase==Phase.Pause||phase==Phase.Inventory||phase==Phase.Map||phase==Phase.Character||phase==Phase.Shop||phase==Phase.Journal||phase==Phase.Dialogue?.48f:1;
        for(int i=0;i<audioMusic.Length;i++){
            audioMusicBlend[i]=Mathf.MoveTowards(audioMusicBlend[i],i==target?1:0,dt*.65f);
            audioMusic[i].volume=(soundOn?musicVolume:0)*audioMusicBlend[i]*quiet;
        }
        // A master/SFX volume change also applies immediately to voices already sounding.
        for(int i=0;i<audioVoices.Length;i++){audioVoices[i].mute=!soundOn||sfxVolume<=0;audioVoices[i].volume=audioVoiceGains[i]*sfxVolume;}
        if(phase!=audioLastPhase){if(phase==Phase.Win)PlaySfx("victory");audioLastPhase=phase;}
        TickFootsteps(dt);
    }
    void TickFootsteps(float dt){
        if(!hero||!motor)return;
        Vector3 difference=hero.position-audioLastPosition;difference.y=0;audioLastPosition=hero.position;
        bool grounded=motor.isGrounded;
        if(phase==Phase.Play){
            if(grounded&&!audioWasGrounded&&difference.sqrMagnitude<4)PlaySfx("footstep",.8f);
            if(grounded&&difference.sqrMagnitude>.0001f&&difference.sqrMagnitude<4&&swing<=0&&dashTime<=0){
                audioFootstepClock-=dt;
                if(audioFootstepClock<=0){PlaySfx("footstep",sprinting?.72f:.5f);audioFootstepClock=sprinting?.23f:.33f;}
            }else audioFootstepClock=0;
        }else audioFootstepClock=0;
        audioWasGrounded=grounded;
    }
    void PlaySfx(string key,float volume=1){PlaySfxAt(key,hero?hero.position:Vector3.zero,volume,false);}
    void PlaySfxAt(string key,Vector3 world,float volume=1,bool positional=true){
        if(!audioReady||!soundOn||smoke||sfxVolume<=0||!audioEffects.TryGetValue(key,out var clip))return;
        // Pickups and multi-target hits cannot stack dozens of identical transients in one frame.
        float gap=key=="coin"?.08f:key=="hit"||key=="boss_hit"?.045f:key=="ui"?.06f:.018f;
        float now=Time.unscaledTime;
        if(audioRecentEffects.TryGetValue(key,out var last)&&now-last<gap)return;
        audioRecentEffects[key]=now;
        float pan=0,attenuation=1;
        if(positional&&hero){Vector3 offset=world-hero.position;float distance=offset.magnitude;attenuation=1/(1+distance*.12f);if(distance>48)return;if(cam)pan=Mathf.Clamp(Vector3.Dot(cam.transform.right,offset.normalized)*.7f,-.7f,.7f);}
        AudioSource voice=null;
        for(int i=0;i<audioVoices.Length;i++){int index=(audioNextVoice+i)%audioVoices.Length;if(!audioVoices[index].isPlaying){voice=audioVoices[index];audioNextVoice=(index+1)%audioVoices.Length;break;}}
        if(!voice){voice=audioVoices[audioNextVoice];audioNextVoice=(audioNextVoice+1)%audioVoices.Length;voice.Stop();}
        int voiceIndex=Array.IndexOf(audioVoices,voice);audioVoiceGains[voiceIndex]=ValidAudioVolume(volume,1)*attenuation;
        voice.clip=clip;voice.volume=audioVoiceGains[voiceIndex]*sfxVolume;voice.panStereo=pan;
        bool melodic=key=="coin"||key=="treasure"||key=="heal"||key=="level"||key=="victory"||key=="ui"||key=="reload_done";
        voice.pitch=melodic?1:1+(float)(audioRandom.NextDouble()-.5)*.08f;voice.mute=false;voice.Play();
    }
    // Compatibility route for existing gameplay events. Replace the old allocating Tone body
    // with PlayLegacySfx(frequency,duration); named PlaySfx calls are preferred for new events.
    void PlayLegacySfx(float frequency,float duration){
        string key=frequency<100?"hurt":frequency<165?"burst":frequency<200?"dodge":frequency<220?"rifle":frequency<250?"reload":frequency<300?"sidearm":frequency<570?"sword"+(Mathf.Clamp(Mathf.RoundToInt((frequency-340)/90),0,2)+1):frequency<680?"reload_done":frequency<740?"heal":frequency<820?"treasure":frequency<950?"treasure":frequency<1020?"coin":duration>.15f?"level":"coin";
        PlaySfx(key);
    }
    void PlayLegacyTone(float frequency,float duration){PlayLegacySfx(frequency,duration);}
    void ValidateAudio(){
        Require(audioReady&&audioMusic!=null&&audioMusic.Length==3,"Audio initialized with three adaptive score layers");
        long decoded=0;
        for(int i=0;i<audioMusic.Length;i++){var source=audioMusic[i];Require(source.clip&&source.clip.samples>0&&source.clip.channels==2,"Stereo score asset loaded");Require(source.loop&&Mathf.Abs(source.clip.length-(i==0?40:20))<.02f,"Score loop duration and repeat configured");decoded+=(long)source.clip.samples*source.clip.channels*4;}
        Require(audioEffects.Count==AudioEffectNames.Length,"All twenty-one cached sound effects loaded");
        foreach(var clip in audioEffects.Values){Require(clip&&clip.samples>0,"Sound effect asset loaded");decoded+=(long)clip.samples*clip.channels*4;}
        Require(decoded<20*1024*1024,"Decoded audio sample budget below 20 MiB");
        Require(audioVoices.Length==10,"Bounded reusable effect voice pool");
        float oldMusic=musicVolume,oldSfx=sfxVolume;SetMusicVolume(2);SetSfxVolume(-1);
        Require(musicVolume==1&&sfxVolume==0,"Audio volumes clamp to valid range");
        SetMusicVolume(float.NaN);SetSfxVolume(float.PositiveInfinity);Require(musicVolume==1&&sfxVolume==0,"Invalid audio volumes cannot reach sources");
        SetMusicVolume(oldMusic);SetSfxVolume(oldSfx);
    }
}
