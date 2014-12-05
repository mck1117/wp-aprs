using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;

namespace WPAPRS
{
    class APRSAudioStreamSource : MediaStreamSource
    {
        const int ChannelCount = 1;
        const int BitsPerSample = 8;
        const int BufferSamples = 2400;
        const int BufferSize = ChannelCount * BufferSamples * BitsPerSample / 8;


        Dictionary<MediaSampleAttributeKeys, string> mediaSampleAttributes;
        MediaStreamDescription mediaStreamDescription;

        int sampleRate;
        long timestamp;

        Queue<Queue<byte>> packetDataQueue = new Queue<Queue<byte>>();
        Queue<byte> currentPacketData = null;
        MemoryStream memoryStream = new MemoryStream();
        Afsk1200Modulator modulator;

        public APRSAudioStreamSource(int sampleRate)
        {
            modulator = new Afsk1200Modulator(sampleRate);

            mediaSampleAttributes = new Dictionary<MediaSampleAttributeKeys, string>();

            this.sampleRate = sampleRate;
        }

        protected override void OpenMediaAsync()
        {
            int byteRate = sampleRate * ChannelCount * BitsPerSample / 8;
            short blockAlign = (short)(ChannelCount * (BitsPerSample / 8));

            // Build string-based wave-format structure
            string waveFormat = "";
            waveFormat += ToLittleEndianString(string.Format("{0:X4}", 1));      // indicates PCM
            waveFormat += ToLittleEndianString(string.Format("{0:X4}", ChannelCount));
            waveFormat += ToLittleEndianString(string.Format("{0:X8}", sampleRate));
            waveFormat += ToLittleEndianString(string.Format("{0:X8}", byteRate));
            waveFormat += ToLittleEndianString(string.Format("{0:X4}", blockAlign));
            waveFormat += ToLittleEndianString(string.Format("{0:X4}", BitsPerSample));
            waveFormat += ToLittleEndianString(string.Format("{0:X4}", 0));

            // Put wave format string in media streams dictionary
            var mediaStreamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
            mediaStreamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = waveFormat;

            // Make description to add to available streams list
            var availableMediaStreams = new List<MediaStreamDescription>();
            mediaStreamDescription = new MediaStreamDescription(MediaStreamType.Audio, mediaStreamAttributes);
            availableMediaStreams.Add(mediaStreamDescription);

            // Set some appropriate keys in the media source dictionary
            var mediaSourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
            mediaSourceAttributes[MediaSourceAttributesKeys.Duration] = "0";
            mediaSourceAttributes[MediaSourceAttributesKeys.CanSeek] = "false";

            // Signal that the open operation is completed
            ReportOpenMediaCompleted(mediaSourceAttributes, availableMediaStreams);
        }

        // For building string-based wave-format structure
        string ToLittleEndianString(string bigEndianString)
        {
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < bigEndianString.Length; i += 2)
                strBuilder.Insert(0, bigEndianString.Substring(i, 2));

            return strBuilder.ToString();
        }


        // Provides audio samples from AudioSampleProvider property
        protected override void GetSampleAsync(MediaStreamType mediaStreamType)
        {
            // Reset memorystream object
            memoryStream.Seek(0, SeekOrigin.Begin);

            int count = 0;

            // Fill as much of the buffer we can with real audio
            while (count <= BufferSamples)
            {
                // If we have no current packet
                if(currentPacketData == null)
                {
                    // If there are packets in the queue, pop the first one
                    if(packetDataQueue.Count > 0)
                    {
                        currentPacketData = packetDataQueue.Dequeue();
                    }
                    else
                    {
                        // If there's nobody in the queue, there's no more real data to add
                        break;
                    }
                }

                // Is the current packet data empty?
                if(currentPacketData.Count > 0)
                {
                    // If not, write a byte
                    memoryStream.WriteByte(currentPacketData.Dequeue());
                    count++;
                }
                else
                {
                    // If it's empty, set to null and finish
                    currentPacketData = null;
                    break;
                }
            }


            // Pad the rest of the buffer with emptiness
            for (int i = count; i < BufferSamples; i++)
            {
                // A neutral value
                memoryStream.WriteByte(127);
            }


            // Send the sample
            ReportGetSampleCompleted(new MediaStreamSample(mediaStreamDescription,
                memoryStream,
                0,
                BufferSize, timestamp, mediaSampleAttributes));

            timestamp += BufferSamples * 10000000L / sampleRate;
        }


        protected override void SeekAsync(long seekToTime)
        {
            ReportSeekCompleted(seekToTime);
        }

        protected override void CloseMedia()
        {
            mediaStreamDescription = null;
        }

        protected override void SwitchMediaStreamAsync(System.Windows.Media.MediaStreamDescription mediaStreamDescription)
        {
            throw new NotImplementedException();
        }

        protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
        {
            throw new NotImplementedException();
        }

        public void EnqueuePacketForTransmission(Packet packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("Cannot transmit a null packet");
            }

            int n = 0;

            modulator.PrepareToTransmit(packet);

            Queue<byte> packetData = new Queue<byte>();

            float[] buf = modulator.GetTxSamplesBuffer();

            while ((n = modulator.GetSamples()) > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    packetData.Enqueue((byte)(buf[i] * 127 + 128));
                }
            }

            packetDataQueue.Enqueue(packetData);
        }
    }
}
