"""Original Teja's Impact score and SFX. Requires Python 3 + NumPy; no samples.
Run from this directory or anywhere: python generate_audio.py.
All oscillator, melody, harmony, percussion and envelope data are authored here.
"""
from pathlib import Path
import hashlib
import json
import math
import wave
import numpy as np

RATE = 22050
BEAT = 60.0 / 96.0
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Resources' / 'Audio'
OUT.mkdir(parents=True, exist_ok=True)
RNG = np.random.default_rng(60627)
manifest = []

def hz(midi):
    return 440 * 2 ** ((midi - 69) / 12)

def env(t, duration, attack=.01, release=.10):
    return np.minimum(1, t / attack) * np.minimum(1, np.maximum(0, duration - t) / release)

def oscillator(midi, duration, instrument):
    t = np.arange(round(duration * RATE)) / RATE
    f = hz(midi)
    phase = 2 * np.pi * (f * t + .0035 * np.sin(2 * np.pi * 4.5 * t))
    if instrument == 'flute':
        sound = np.sin(phase) + .19 * np.sin(2*phase) + .045 * np.sin(3*phase)
        return sound * env(t, duration, .055, .23) * (.75 + .25*np.exp(-t))
    if instrument == 'pad':
        sound = np.sin(phase) + .27*np.sin(phase*2) + .10*np.sin(phase*3)
        sound += .19*np.sin(2*np.pi*f*1.002*t)
        return sound * env(t, duration, .38, .7)
    if instrument == 'pluck':
        sound = np.sin(phase) + .35*np.sin(phase*2)*np.exp(-t*5) + .15*np.sin(phase*3)*np.exp(-t*10)
        return sound * np.exp(-t*3.0) * env(t,duration,.004,.12)
    if instrument == 'bell':
        sound = np.sin(phase)+.3*np.sin(phase*2.007)+.09*np.sin(phase*3.996)
        return sound * np.exp(-t*2.2) * env(t,duration,.008,.22)
    if instrument == 'bass':
        return (np.sin(phase)+.16*np.sin(phase*2)) * env(t,duration,.015,.14) * (.65+.35*np.exp(-t*3))
    return np.sin(phase) * env(t,duration)

def place(buffer, mono, seconds, volume, pan=0, wrap=True):
    start = round(seconds*RATE)
    gains=np.array([math.sqrt((1-pan)/2),math.sqrt((1+pan)/2)])
    indices=np.arange(start,start+len(mono))
    if wrap:
        # Tails cross the loop boundary; there is no artificial silence at the seam.
        np.add.at(buffer, indices % len(buffer), mono[:,None]*gains[None,:]*volume)
    else:
        keep=indices < len(buffer)
        buffer[indices[keep]] += mono[keep,None]*gains[None,:]*volume

def note(buffer, midi, beat, beats, voice, volume, pan=0):
    place(buffer,oscillator(midi,beats*BEAT,voice),beat*BEAT,volume,pan)

def drum(kind, duration=.30):
    t=np.arange(round(RATE*duration))/RATE
    noise=RNG.uniform(-1,1,len(t))
    if kind=='kick':
        phase=2*np.pi*(48*t+52*.025*(1-np.exp(-t/.025)))
        return np.sin(phase)*np.exp(-t*13)*env(t,duration,.001,.03)+noise*.12*np.exp(-t*60)
    if kind=='snare':
        return (noise*.45+np.sin(2*np.pi*180*t)*.27)*np.exp(-t*22)*env(t,duration,.001,.035)
    return (noise-np.roll(noise,1))*.22*np.exp(-t*48)*env(t,duration,.002,.035)

def finish_music(buffer):
    # Short circular stereo reflections create width without a large reverb tail.
    wet=buffer.copy()
    for delay,level in ((.1875,.13),(.3125,.09),(.50,.045)):
        wet += np.roll(buffer[:,::-1],round(delay*RATE),axis=0)*level
    peak=float(np.max(np.abs(wet)))
    wet *= .73/max(peak,.01)
    return wet.astype(np.float32)

def write(name,samples,loop=False):
    samples=np.asarray(samples,dtype=np.float32)
    if samples.ndim==1: samples=samples[:,None]
    assert np.all(np.isfinite(samples))
    if not loop:
        fade=min(round(.012*RATE),len(samples)//4)
        samples[:fade] *= np.linspace(0,1,fade)[:,None]
        samples[-fade:] *= np.linspace(1,0,fade)[:,None]
    peak=float(np.max(np.abs(samples)))
    if peak>.86: samples *= .86/peak
    pcm=(np.clip(samples,-1,1)*32767).astype('<i2')
    destination=OUT/(name+'.wav')
    with wave.open(str(destination),'wb') as stream:
        stream.setnchannels(samples.shape[1]);stream.setsampwidth(2);stream.setframerate(RATE);stream.writeframes(pcm.tobytes())
    manifest.append({'name':name,'file':destination.name,'seconds':len(samples)/RATE,'channels':samples.shape[1],
                     'rate':RATE,'loop':loop,'peak':round(float(np.max(np.abs(samples))),6),
                     'rms':round(float(np.sqrt(np.mean(samples*samples))),6),
                     'decoded_float_bytes':int(samples.size*4),'sha256':hashlib.sha256(destination.read_bytes()).hexdigest()})

# Skybound Morning: D major, 16 bars, evolving flute phrase above harp and warm pads.
chords=[(50,54,57,61),(47,50,54,57),(43,47,50,54),(45,49,52,57)]
phrases=[[(0,74,1), (1,78,.5),(1.5,76,.5),(2,81,1.5),(3.5,78,.5)],
         [(0,78,1.5),(1.5,76,.5),(2,74,1),(3,71,1)],
         [(0,74,1),(1,76,.5),(1.5,78,.5),(2,79,1),(3,78,.5),(3.5,74,.5)],
         [(0,76,1.5),(1.5,73,.5),(2,69,1),(3,73,1)],
         [(0,81,1),(1,78,1),(2,76,.5),(2.5,74,.5),(3,78,1)],
         [(0,78,.5),(1,81,1),(2,83,1),(3,81,.5),(3.5,78,.5)],
         [(0,79,1.5),(1.5,78,.5),(2,74,1),(3,71,1)],
         [(0,73,1),(1,76,.5),(1.5,78,.5),(2,76,1),(3,74,1)]]
explore=np.zeros((round(64*BEAT*RATE),2),np.float64)
for bar in range(16):
    chord=chords[bar%4]
    for i,midi in enumerate(chord): note(explore,midi+12,bar*4,4.6,'pad',.034,(-.45+i*.3))
    note(explore,chord[0]-12,bar*4,3.9,'bass',.075,-.1)
    for step in range(8):
        n=chord[[0,1,2,3,2,1,3,2][step]]+24
        note(explore,n,bar*4+step*.5,1.6,'pluck',.052,(-.5 if step%2==0 else .5))
    phrase=phrases[bar%8]
    for offset,midi,hold in phrase:
        note(explore,midi+(12 if bar in (10,11) else 0),bar*4+offset,hold+.18,'flute',.072,.12)
    if bar%4==0: note(explore,86,bar*4+.05,2.6,'bell',.028,-.35)
    for step in (1,3): place(explore,drum('shaker',.13),(bar*4+step+.5)*BEAT,.055,(-.3 if step==1 else .3))
write('music-exploration',finish_music(explore),True)

# Aether Pursuit: B minor pulse, faster note values at the same tempo, eight bars.
combat=np.zeros((round(32*BEAT*RATE),2),np.float64)
combat_chords=[(47,50,54),(43,47,50),(50,54,57),(45,49,52)]
for bar in range(8):
    chord=combat_chords[bar%4]
    for i,midi in enumerate(chord): note(combat,midi+12,bar*4,4.1,'pad',.028,-.5+i*.5)
    for step in range(8):
        note(combat,chord[step%3]+12,bar*4+step*.5,.52,'pluck',.09,(-.3 if step%2==0 else .3))
        note(combat,chord[0]-12,bar*4+step*.5,.44,'bass',.07)
        place(combat,drum('shaker',.11),(bar*4+step*.5)*BEAT,.13,(-.5 if step%2 else .5))
    for beat in (0,2): place(combat,drum('kick'),(bar*4+beat)*BEAT,.31)
    for beat in (1,3): place(combat,drum('snare',.2),(bar*4+beat)*BEAT,.21,.05)
    for offset,midi,hold in phrases[(bar+4)%8]: note(combat,midi,bar*4+offset,max(.25,hold*.7),'bell',.055,-.1)
write('music-combat',finish_music(combat),True)

# Crown of the Rift: B minor / F# dominant, eight bars of low drums and rising bells.
boss=np.zeros((round(32*BEAT*RATE),2),np.float64)
boss_chords=[(47,50,54),(43,47,50),(40,43,47),(42,46,49)]
for bar in range(8):
    chord=boss_chords[bar%4]
    for i,midi in enumerate(chord):
        note(boss,midi+12,bar*4,4.4,'pad',.045,(-.4+i*.4))
        note(boss,midi+24,bar*4+2,1.9,'flute',.023,.35)
    for step in range(16):
        note(boss,chord[(step//2)%3]+12,bar*4+step*.25,.35,'pluck',.058,-.3 if step%2 else .3)
    for beat in (0,1.5,2,3.5):
        note(boss,chord[0]-12,bar*4+beat,.7,'bass',.17)
        place(boss,drum('kick',.38),(bar*4+beat)*BEAT,.37)
    for beat in (1,3): place(boss,drum('snare',.25),(bar*4+beat)*BEAT,.25,-.1)
    for step in range(8): place(boss,drum('shaker',.13),(bar*4+step*.5)*BEAT,.13,.4)
    for k in range(4): note(boss,chord[k%3]+24+(12 if k==3 else 0),bar*4+k,1.3,'bell',.07,.15)
write('music-boss',finish_music(boss),True)

def effect(duration):
    t=np.arange(round(duration*RATE))/RATE
    return t,np.zeros(len(t),np.float64)

def chime(name,notes,duration=.8,spacing=.09):
    t,out=effect(duration)
    for i,midi in enumerate(notes):
        snd=oscillator(midi,max(.1,duration-i*spacing),'bell')*.25
        at=round(i*spacing*RATE);out[at:at+len(snd)] += snd[:len(out)-at]
    write(name,out)

for index,duration in enumerate((.24,.28,.36),1):
    t,out=effect(duration);noise=RNG.uniform(-1,1,len(t));soft=np.convolve(noise,np.ones(5)/5,'same')
    sweep=np.sin(2*np.pi*(1050*t-1100*t*t))
    out=(soft*.62+sweep*.11)*np.sin(np.pi*t/duration)**1.5
    out+=np.sin(2*np.pi*(160+index*22)*t)*np.exp(-t*18)*.12
    write('sword'+str(index),out)
for name,duration,pitch in [('sidearm',.26,580),('rifle',.18,740)]:
    t,out=effect(duration);noise=RNG.uniform(-1,1,len(t))
    phase=2*np.pi*(pitch*.035*(1-np.exp(-t/.035))+110*t)
    out=np.sin(phase)*np.exp(-t*16)*.51+noise*np.exp(-t*65)*.23
    write(name,out)
t,out=effect(.65)
for at in (.0,.23,.43):
    k=t-at;mask=k>=0
    out[mask]+=(RNG.uniform(-1,1,np.count_nonzero(mask))*.18+np.sin(2*np.pi*840*k[mask])*.10)*np.exp(-k[mask]*45)
out+=np.sin(2*np.pi*(220*t+360*t*t))*np.sin(np.pi*t/.65)*.11
write('reload',out)
chime('reload_done',[78,85],.42,.075)
t,out=effect(.22);out=(RNG.uniform(-1,1,len(t))*.24+np.sin(2*np.pi*(125*t+220*t*t))*.45)*np.exp(-t*25);write('hit',out)
t,out=effect(.37);noise=RNG.uniform(-1,1,len(t));out=(np.sin(2*np.pi*(72*t+90*t*t))*.40+np.sin(2*np.pi*420*t)*.13+noise*.19)*np.exp(-t*15);write('boss_hit',out)
t,out=effect(.34);out=(np.sin(2*np.pi*(150*t-95*t*t))*.38+RNG.uniform(-1,1,len(t))*.13)*np.exp(-t*10);write('hurt',out)
chime('coin',[86,93],.31,.055)
chime('treasure',[74,78,81,86],1.2,.10)
chime('heal',[74,78,81],.9,.13)
chime('level',[74,78,81,86,90],1.5,.13)
chime('victory',[62,66,69,74,78,81,86],2.4,.16)
chime('ui',[78],.16,.1)
for name,duration,pitch,volume in [('jump',.26,370,.28),('dodge',.24,180,.22),('footstep',.10,110,.23)]:
    t,out=effect(duration);noise=RNG.uniform(-1,1,len(t));filtered=np.convolve(noise,np.ones(7)/7,'same')
    out=(filtered*.65+np.sin(2*np.pi*(pitch*t+250*t*t))*.35)*np.sin(np.pi*t/duration)*volume
    write(name,out)
t,out=effect(.95);noise=RNG.uniform(-1,1,len(t))
out=(np.sin(2*np.pi*(60*t+110*t*t))*.31+np.sin(2*np.pi*660*t)*.13+noise*.12)*env(t,.95,.025,.45)*np.exp(-t*2)
write('burst',out)
t,out=effect(.65);out=(np.sin(2*np.pi*85*t)*.35+np.sin(2*np.pi*127.5*t)*.15)*env(t,.65,.04,.35);write('warning',out)

decoded=sum(m['decoded_float_bytes'] for m in manifest)
assert decoded < 20*1024*1024,decoded
assert len(manifest)==24
assert all(m['peak']<=.861 and m['rms']>.005 for m in manifest)
(OUT/'AUDIO-MANIFEST.json').write_text(json.dumps({'origin':'Original procedural composition and synthesis; no external samples','decoded_float_bytes':decoded,'clips':manifest},indent=2)+'\n',encoding='utf-8')
print(f'Generated {len(manifest)} clips; decoded float sample budget {decoded/1024/1024:.2f} MiB.')
for row in manifest: print(row['name'],row['seconds'],row['channels'],row['peak'],row['rms'])
