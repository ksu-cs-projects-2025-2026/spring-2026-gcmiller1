using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Codecs;

namespace AgentView
{
    public class AudioService
    {
        private WaveOutEvent waveOut;
        private BufferedWaveProvider bufferProvider;
        private WaveInEvent waveIn;
        private bool micMuted;

        /// <summary>
        /// Starts the audio playback for the phone call
        /// </summary>
        public void StartPlayback()
        {
            waveOut = new WaveOutEvent();
            bufferProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1))
            {
                BufferDuration = TimeSpan.FromSeconds(5),
                DiscardOnBufferOverflow = true
            };

            waveOut.Init(bufferProvider);
            waveOut.Play();
        }

        /// <summary>
        /// Sets the muted state of the agent microphone
        /// </summary>
        /// <param name="mute">if the mic is being muted or unmuted</param>
        public void SetMute(bool mute)
        {
            micMuted = mute;
        }

        /// <summary>
        /// Decodes the phone call audio bytes being received from the server
        /// </summary>
        /// <param name="muLawBytes"></param>
        public void PlayMuLawAudio(byte[] muLawBytes)
        {
            var pcmBuffer = new byte[muLawBytes.Length * 2];

            for (int i = 0; i < muLawBytes.Length; i++)
            {
                short pcm = MuLawDecoder.MuLawToLinearSample(muLawBytes[i]);
                pcmBuffer[i * 2] = (byte)(pcm & 0xff);
                pcmBuffer[i * 2 + 1] = (byte)((pcm >> 8) & 0xff);
            }

            bufferProvider?.AddSamples(pcmBuffer, 0, pcmBuffer.Length);
        }

        /// <summary>
        /// Begins capturing the agent microphone audio
        /// </summary>
        /// <param name="onMuLawReady"></param>
        public void StartMicCapture(Func<byte[], Task> onMuLawReady)
        {
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(8000, 16, 1)
            };

            waveIn.DataAvailable += async (s, e) =>
            {
                if (micMuted)
                {
                    return;
                }

                var pcm = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, pcm, e.BytesRecorded);

                var muLaw = new byte[pcm.Length / 2];
                for (int i = 0; i < muLaw.Length; i++)
                {
                    short sample = BitConverter.ToInt16(pcm, i * 2);
                    muLaw[i] = MuLawEncoder.LinearToMuLawSample(sample);
                }

                await onMuLawReady(muLaw);
            };

            waveIn.StartRecording();
        }

        /// <summary>
        /// Stops the audio input and output
        /// </summary>
        public void Stop()
        {
            waveIn?.StopRecording();
            waveOut?.Stop();
        }
    }
}
