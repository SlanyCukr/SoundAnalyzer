using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Net.Http;
using System.Windows.Threading;

namespace SoundAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, MMDevice> DevicesDictionary { get; set; }
        private WasapiLoopbackCapture CurrentCapture { get; set; }
        private int LoopRunCount { get; set; }
   
        public MainWindow()
        {
            InitializeComponent();

            DevicesDictionary = new Dictionary<string, MMDevice>();
            LoopRunCount = 0;
            CurrentCapture = null;

            // find output devices and display them in combo box
            var enumerator = new MMDeviceEnumerator();
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
                if(device.State == DeviceState.Active && device.DataFlow == DataFlow.Render)
                    DevicesDictionary.Add(device.FriendlyName, device);
            devicesComboBox.ItemsSource = DevicesDictionary.Keys;
            devicesComboBox.SelectedIndex = 0;
        }

        private void startAnalyzingButton_Click(object sender, RoutedEventArgs e)
        {
            // find device in dictionary
            string deviceName = (string)devicesComboBox.SelectedItem;
            var selectedDevice = DevicesDictionary[deviceName];

            // stop recording if another capture was already assigned to this variable
            if (CurrentCapture != null)
                CurrentCapture.StopRecording();

            CurrentCapture = new WasapiLoopbackCapture(selectedDevice);
            CurrentCapture.DataAvailable += (s, a) =>
            {
                // don't read data constantly
                if (LoopRunCount < 25)
                {
                    LoopRunCount += 1;
                    Thread.Sleep(1);
                    return;
                }
                LoopRunCount = 0;

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
                for (int i = 0; i < convertedValues.Count; i++)
                {
                    sum += convertedValues[i];

                    // if we need to go to next index in condensedBuffer
                    if (i % newCondensedBufferIndex == 0)
                    {
                        var avgValue = sum / newCondensedBufferIndex;
                        condensedBuffer.Add(avgValue);
                        condensedBufferStr += avgValue + ";";
                        sum = 0;
                    }
                }

                // send data to LED strip
                condensedBufferStr = condensedBufferStr.Remove(condensedBufferStr.Length - 1);
                //LedAPI.RealTime(condensedBufferStr);
            };
            CurrentCapture.StartRecording();
        }
    }
}
