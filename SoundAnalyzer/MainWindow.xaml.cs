using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Net.Http;

namespace SoundAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MMDevice captureDevice = null;
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
            {
                if (wasapi.State == DeviceState.Active && wasapi.DataFlow == DataFlow.Render && wasapi.FriendlyName.Equals("Reproduktory (High Definition Audio Device)"))
                {
                    try
                    {
                        captureDevice = wasapi;
                        Console.WriteLine($"{wasapi.FriendlyName} {wasapi.DeviceFriendlyName}");
                    }
                    catch (Exception) { }
                }
            }

            var capture = new WasapiLoopbackCapture(captureDevice);
            HttpClient Client = new HttpClient();
            capture.DataAvailable += (s, a) =>
            {
                List<int> convertedValues = new List<int>();
                var buffer = new WaveBuffer(a.Buffer);

                // interpret as 32 bit floating point audio
                for (int index = 0; index < a.BytesRecorded / 4; index++)
                {
                    var sample = buffer.FloatBuffer[index];

                    // absolute value 
                    if (sample < 0) sample = -sample;
                    int scaledValue = (int)(sample * 255);
                    convertedValues.Add(scaledValue);
                }

                List<int> condensedBuffer = new List<int>();
                int newCondensedBufferIndex = convertedValues.Count / (177);
                string condensedBufferStr = "";

                int sum = 0;
                for(int i = 0; i < convertedValues.Count; i++)
                {
                    sum += convertedValues[i];

                    // if we need to go to next index in condensedBuffer
                    if(i % newCondensedBufferIndex == 0)
                    {
                        var avgValue = sum / newCondensedBufferIndex;
                        condensedBuffer.Add(avgValue);
                        condensedBufferStr += avgValue + ";";
                        sum = 0;
                    }
                }

                // send data to LED strip
                condensedBufferStr = condensedBufferStr.Remove(condensedBufferStr.Length - 1);
                var encodedValues = new FormUrlEncodedContent(new Dictionary<string, string> { { "values", condensedBufferStr} });
                Client.PostAsync("http://192.168.0.114:5000/real_time", encodedValues);
            };
            capture.StartRecording();
            while (capture.CaptureState != CaptureState.Stopped)
                Thread.Sleep(25);
        }
    }
}
