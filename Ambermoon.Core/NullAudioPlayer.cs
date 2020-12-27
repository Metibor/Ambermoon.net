using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using System;

namespace Ambermoon
{
    public class NullAudioPlayer : IAudioPlayer
    {
        public void PlayTrack(AudioTrack musicIndex)
        {
            Console.WriteLine($"Playing music track: {musicIndex}");
        }

        public void Pause()
        {
            Console.WriteLine("Playback paused");
        }

        public void Play()
        {
            Console.WriteLine("Playback started");
        }

        public void Stop()
        {
            Console.WriteLine("Playback stopped");
        }

        public void SetVolume(float volume)
        {
            Console.WriteLine($"Setting volume to: {volume}");
        }

        public void Path(string path)
        {
            Console.WriteLine($"Setting path to: {path}");
        }
    }
}