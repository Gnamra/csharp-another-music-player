﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Timers;
using NAudio.Wave;
using PluginContracts;

namespace AMPGUI.Models
{
    /// <summary>
    /// Represents a playback. You can play, pause, stop and seek through a playback.
    /// </summary>
    public class Playback : IDisposable
    {
        /// <summary>
        /// Current time of playback
        /// </summary>
        public TimeSpan Time { get { return WaveStream.CurrentTime; } }

        /// <summary>
        /// Length of the audio file
        /// </summary>
        public TimeSpan TotalTime { get { return WaveStream.TotalTime; } }
        /// <summary>
        /// Timer that updates once a second.
        /// Can be used to track the progress of the song.
        /// </summary>
        public Timer TimeTracker { get;  }
        public long Position
        {
            get { return Decoder.GetWaveStream().Position; }
            set { Decoder.GetWaveStream().Position = value; }
        }
        public float Volume
        {
            get { return WaveOut.Volume; }
            set { WaveOut.Volume = value; }
        }
        public PlaybackState State { get { return WaveOut.PlaybackState; } }
        private IDecoder Decoder { get; }
        private WaveStream WaveStream { get; }
        private WaveOutEvent WaveOut { get; }

        // Events
        public event EventHandler PlaybackStopped;
        public event EventHandler Elapsed;

        public Playback(string pathToSong, DecoderLoader dl)
        {
            if (pathToSong == null)
                return;

            Decoder = dl?.GetDecoder(pathToSong);
            WaveStream = Decoder.GetWaveStream();
            WaveOut = new WaveOutEvent { NumberOfBuffers = 2};
            WaveOut.Init(WaveStream);
            WaveOut.PlaybackStopped += WaveOut_PlaybackStopped;

            TimeTracker = new Timer(200)
            {
                AutoReset = true
            };
            TimeTracker.Elapsed += TimeTracker_Elapsed;

        }

        public void Stop()
        {
            Dispose();
        }
        public void Play()
        {
            TimeTracker.Enabled = true;
            WaveOut.Play();
        }
        public void Pause()
        {
            WaveOut.Pause();
        }
        public void Seek(string seekTime)
        {
            try
            {
                string[] timeSplit = seekTime?.Contains(':') == true ?
                    seekTime.Split(':') :
                    throw new Exception("Input string was not in a correct format.");

                TimeSpan time = TimeSpan.ParseExact(seekTime, "%m\\:%s", CultureInfo.InvariantCulture);
               
                Decoder.Seek(time.Minutes * 60 + time.Seconds);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Dispose()
        {
            TimeTracker.Elapsed -= TimeTracker_Elapsed;
            TimeTracker.Stop();

            WaveOut.PlaybackStopped -= WaveOut_PlaybackStopped;
            WaveOut.Stop();
            
            TimeTracker.Dispose();
            WaveOut.Dispose();
            WaveStream.Dispose();
        }

        #region EventHandler methods
        private void TimeTracker_Elapsed(object sender, ElapsedEventArgs e)
        {
            Elapsed?.Invoke(sender, e);
        }

        // Expose the playback stopped event.
        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(sender, e);
            Dispose();
        }
        #endregion

    }
}
