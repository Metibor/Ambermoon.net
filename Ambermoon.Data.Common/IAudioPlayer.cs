using Ambermoon.Data.Enumerations;

namespace Ambermoon.Data
{
    public interface IAudioPlayer
    {
        void PlayTrack(AudioTrack musicIndex);

        void SetVolume(float volume);

        void Play();

        void Pause();

        void Stop();

        void Path(string path);
    }
}
