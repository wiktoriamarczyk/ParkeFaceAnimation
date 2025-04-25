#pragma strict

public var impact: AudioClip;
var lipsync_audio: AudioSource;
function Start() {
    lipsync_audio = GetComponent.<AudioSource>();
}
function PlayAudio() {
    lipsync_audio.PlayOneShot(impact);
}
function StopAudio() {
    lipsync_audio.Stop();
}