"""Validate the delivered PCM assets against their manifest (Python + NumPy)."""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

root=Path(__file__).resolve().parents[1]/'Resources'/'Audio'
manifest=json.loads((root/'AUDIO-MANIFEST.json').read_text(encoding='utf-8'))
assert len(manifest['clips'])==24
decoded=0
results=[]
for clip in manifest['clips']:
    file=root/clip['file']
    assert hashlib.sha256(file.read_bytes()).hexdigest()==clip['sha256'],file.name
    with wave.open(str(file),'rb') as stream:
        assert stream.getframerate()==22050 and stream.getsampwidth()==2
        assert stream.getnchannels()==clip['channels']
        samples=np.frombuffer(stream.readframes(stream.getnframes()),dtype='<i2').astype(np.float64).reshape(-1,clip['channels'])/32768
    assert abs(len(samples)/22050-clip['seconds'])<1/22050
    peak=float(np.max(np.abs(samples)))
    rms=float(np.sqrt(np.mean(samples*samples)))
    assert .01<peak<.861 and rms>.005,file.name
    assert abs(peak-clip['peak'])<.0001
    assert abs(rms-clip['rms'])<.0001
    assert np.max(np.abs(np.mean(samples,axis=0)))<.005,file.name
    decoded+=samples.size*4
    row={'file':file.name,'seconds':len(samples)/22050,'peak':round(peak,6),'rms':round(rms,6)}
    if clip['loop']:
        assert clip['channels']==2
        assert len(samples)/22050 in (20,40)
        seam=float(np.max(np.abs(samples[0]-samples[-1])))
        usual_transient=float(np.quantile(np.abs(np.diff(samples,axis=0)),.99))
        assert seam<usual_transient,file.name
        row.update(seam_step=round(seam,6),adjacent_step_99th_percentile=round(usual_transient,6))
    else:
        assert np.all(samples[0]==0) and np.all(samples[-1]==0),file.name
    results.append(row)
assert decoded==manifest['decoded_float_bytes'] and decoded<20*1024*1024
output={'result':'PASS','clips':len(results),'decoded_float_bytes':decoded,'checks':'WAV format, hashes, durations, peaks, RMS, DC offset, transient-bounded music seams, zero-endpoint effects, memory budget','assets':results}
destination=Path(__file__).resolve().parent/'AUDIO-VALIDATION.json'
destination.write_text(json.dumps(output,indent=2)+'\n',encoding='utf-8')
print(f'PASS: {len(results)} clips; decoded sample data {decoded/1024/1024:.2f} MiB; all manifest hashes and waveform checks match.')
