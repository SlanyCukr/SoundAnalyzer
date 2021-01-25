using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Net.Http;
using System.Windows.Threading;
using System.Linq;

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
        private int[] FFT(int[] data)
        {
            int[] fft = new int[data.Length];
            System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];
            for (int i = 0; i < data.Length; i++)
                fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);
            Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);
            for (int i = 0; i < data.Length; i++)
                fft[i] = (int)(fftComplex[i].Magnitude);
            return fft;
        }

        private void startAnalyzingButton_Click(object sender, RoutedEventArgs e)
        {
            // find device in dictionary
            string deviceName = (string)devicesComboBox.SelectedItem;
            var selectedDevice = DevicesDictionary[deviceName];

            // stop recording if another capture was already assigned to this variable
            if (CurrentCapture != null)
            {
                CurrentCapture.StopRecording();
                CurrentCapture = null;
            }

            CurrentCapture = new WasapiLoopbackCapture(selectedDevice);
            CurrentCapture.DataAvailable += (s, a) =>
            {
                // don't read data constantly
                /*if (LoopRunCount < 2)
                {
                    LoopRunCount += 1;
                    Thread.Sleep(1);
                    return;
                }
                LoopRunCount = 0;*/

                Thread.Sleep(10);

                int[] convertedValues = new int[(int)Math.Pow(2, 14)];
                var buffer = new WaveBuffer(a.Buffer);

                // interpret as 32 bit floating point audio
                for (int index = 0; index < a.BytesRecorded / 4; index++)
                {
                    if (index >= (int)Math.Pow(2, 14))
                        continue;

                    var sample = buffer.FloatBuffer[index];

                    // absolute value 
                    if (sample < 0) sample = -sample;
                    //int scaledValue = (int)(sample * 255);
                    int scaledValue = (int)(sample * 255);
                    convertedValues[index] = scaledValue;
                }

                var test = convertedValues.Max();


                // fast fourier transformed values to extract different sound levels on different frequencies
                var fftArr = FFT(convertedValues);

                List<int> condensedBuffer = new List<int>();
                //int newCondensedBufferIndex = convertedValues.Count / (177);
                int newCondensedBufferIndex = convertedValues.Length / 177;
                string condensedBufferStr = "";

                int sum = 0;
                for (int i = 0; i < fftArr.Length; i++)
                {
                    sum += fftArr[i];

                    // if we need to go to next index in condensedBuffer
                    if (i % newCondensedBufferIndex == 0 && i > 0)
                    {
                        var avgValue = sum / newCondensedBufferIndex;

                        /*for (int j = 0; j < 8; j++)
                        {
                            condensedBuffer.Add(avgValue);
                            condensedBufferStr += avgValue + ";";
                        }*/
                        condensedBuffer.Add(avgValue);
                        condensedBufferStr += avgValue + ";";
                        sum = 0;
                    }
                }

                // send data to LED strip
                if (condensedBufferStr != String.Empty)
                {
                    condensedBufferStr = condensedBufferStr.Remove(condensedBufferStr.Length - 1);
                    LedAPI.RealTime(condensedBufferStr);
                }
            };
            CurrentCapture.StartRecording();
        }
    }
}
