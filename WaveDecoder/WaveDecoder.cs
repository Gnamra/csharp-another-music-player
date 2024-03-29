﻿using System;
using System.IO;
using NAudio.Wave;
using PluginContracts;

namespace WaveDecoder
{
    /// <summary>
    /// Decodes and provides information about Wave files
    /// </summary>
    class WaveDecoder : WaveStream, IDecoder, IDisposable
    {
        private readonly string readLock = "";
        private BinaryReader AudioFileReader { get; set; }
        public WaveAudioDataFormat AudioFile { get; set; }
        private string CurrentChunkId { get; set; }
        private int CurrentChunkSize { get; set; }
        private int CurrentChunkStart { get; set; }

        public override WaveFormat WaveFormat { get; }

        public override long Length { get { return AudioFile.Data.Data.Length; } }

        public override long Position { get; set; }

        public WaveDecoder(string file)
        {
            CurrentChunkId = "";
            CurrentChunkSize = 0;
            AudioFile = new WaveAudioDataFormat();
            try
            {
                using (AudioFileReader = new BinaryReader(File.OpenRead(file)))
                {
                    AudioFile.RIff = ExtractRiffData();
                    AudioFile.Format = ExctractFormatData();
                    AudioFile.Data = ExtractAudioData();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Program shutting down.");
                Environment.Exit(1);
            }

            WaveFormat = new WaveFormat(AudioFile.Format.SampleRate, AudioFile.Format.NumChannels);
        }
        private void NextChunk()
        {
            // Add check to see if at the start of a chunk or not

            CurrentChunkStart = (int)AudioFileReader.BaseStream.Position;
            CurrentChunkId = new string(AudioFileReader.ReadChars(4));
            Console.WriteLine(CurrentChunkId);

            if (CurrentChunkId.Equals("RIFF"))
            {
                CurrentChunkSize = AudioFileReader.ReadInt32();
            }
            else
            {
                // Doesn't account for pad bytes
                CurrentChunkSize = AudioFileReader.ReadInt32();
                AudioFileReader.ReadBytes(CurrentChunkSize);
            }
        }
        private WaveAudioDataFormat.RiffChunk ExtractRiffData()
        {
            WaveAudioDataFormat.RiffChunk data = new WaveAudioDataFormat.RiffChunk
            {
                ID = new string(AudioFileReader.ReadChars(4)),
                Size = AudioFileReader.ReadInt32(),
                Format = new string(AudioFileReader.ReadChars(4))
            };

            return data;
        }
        private WaveAudioDataFormat.FormatChunk ExctractFormatData()
        {
            while (!CurrentChunkId.Equals("fmt ")) { NextChunk(); };
            AudioFileReader.BaseStream.Position = CurrentChunkStart + 8;
            WaveAudioDataFormat.FormatChunk data = new WaveAudioDataFormat.FormatChunk
            {
                ID = CurrentChunkId,
                Size = CurrentChunkSize,
                AudioFormat = AudioFileReader.ReadUInt16(),
                NumChannels = AudioFileReader.ReadUInt16(),
                SampleRate = AudioFileReader.ReadInt32(),
                ByteRate = AudioFileReader.ReadInt32(),
                BlockAlign = AudioFileReader.ReadUInt16(),
                BitsPerSample = AudioFileReader.ReadUInt16()
            };
            Console.WriteLine("========= FORMAT CHUNK =========");
            Console.WriteLine("0: Chunk id: " + new string(data.ID));
            Console.WriteLine($"4: File size: {data.Size}");
            Console.WriteLine($"6: Audio format: {data.AudioFormat}");
            Console.WriteLine($"8: Channels: {data.NumChannels}");
            Console.WriteLine($"12: Sample rate: {data.SampleRate}");
            Console.WriteLine($"16: Byte rate: {data.ByteRate}");
            Console.WriteLine($"18: Block alignment: {data.BlockAlign}");
            Console.WriteLine($"20: Bits per sample: {data.BitsPerSample}");

            return data;
        }
        private WaveAudioDataFormat.DataChunk ExtractAudioData()
        {
            CurrentChunkStart = (int)AudioFileReader.BaseStream.Position;
            while (!CurrentChunkId.Equals("data")) { NextChunk(); };
            AudioFileReader.BaseStream.Position = CurrentChunkStart;
            WaveAudioDataFormat.DataChunk data = new WaveAudioDataFormat.DataChunk
            {
                ID = CurrentChunkId,
                Size = CurrentChunkSize,
                Data = AudioFileReader.ReadBytes(CurrentChunkSize)
            };
            Console.WriteLine("========= DATA CHUNK =========");
            Console.WriteLine("0: Chunk id: " + data.ID);
            Console.WriteLine($"4: Chunk size: {data.Size}");
            Console.WriteLine($"8: Chunk Data: {data.Data}");
            return data;

        }

        public string GetName()
        {
            return "WaveDecoder";
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (readLock)
            {
                Buffer.BlockCopy(AudioFile.Data.Data, (int)Position, buffer, offset, count);
                Position += buffer.Length;
                return buffer.Length;
            }
        }

        public WaveStream GetWaveStream()
        {
            return this;
        }

        public void Seek(int time)
        {
            Position = time * AudioFile.Format.ByteRate;
        }
    }
}
