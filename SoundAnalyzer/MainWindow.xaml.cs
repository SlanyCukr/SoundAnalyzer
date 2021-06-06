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

        private int[] PreviousSums { get; set; }
        private int PreviousSumIndex { get; set; }
        private int PreviousSum { get; set; }
   
        public MainWindow()
        {
            InitializeComponent();


            DevicesDictionary = new Dictionary<string, MMDevice>();
            CurrentCapture = null;
            PreviousSums = new int[5] { 0, 0, 0, 0, 0 };
            PreviousSumIndex = 0;
            PreviousSum = 0;

            // find output devices and display them in combo box
            var enumerator = new MMDeviceEnumerator();
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
                if(device.State == DeviceState.Active && device.DataFlow == DataFlow.Render)
                    DevicesDictionary.Add(device.FriendlyName, device);
            devicesComboBox.ItemsSource = DevicesDictionary.Keys;
            devicesComboBox.SelectedIndex = 0;
        }
        private double[] FFT(float[] data)
        {
            double[] fft = new double[data.Length];
            System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];
            for (int i = 0; i < data.Length; i++)
                fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);
            Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);
            for (int i = 0; i < data.Length; i++)
                fft[i] = fftComplex[i].Magnitude;
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
                Thread.Sleep(5);

                float[] interpretedValues = InterpretBytesAsFloat(a.Buffer);

                //float[] condensedInterpretedValues = CondenseArray(interpretedValues, (int)Math.Pow(2,14));

                // fast fourier transformed values to extract different sound levels on different frequencies
                //var fftArr = FFT(condensedInterpretedValues);

                /*int newCondensedBufferIndex = (fftArr.Length / 177) / 90;
                string condensedBufferStr = "";

                int sum = 0;
                List<int> templist = new List<int>();
                for (int i = 0; i < fftArr.Length / 90; i++)
                {
                    sum += (int)(fftArr[i] * 25500);
                    templist.Add((int)(fftArr[i] * 25500));
                    // if we need to go to next index in condensedBuffer
                    if (i % newCondensedBufferIndex == 0 && i > 0)
                    {
                        var avgValue = sum / newCondensedBufferIndex;
                        //var avgValue = templist.Max();// / (templist.Min() + 1);

                        /*for (int j = 0; j < 8; j++)
                        {
                            condensedBuffer.Add(avgValue);
                            condensedBufferStr += avgValue + ";";
                        }
                        condensedBufferStr += avgValue + ";";
                        sum = 0;
                        templist.Clear();
                    }
                }*/
                List<byte> values = new List<byte>();
                
                //int sum = (int)(interpretedValues.Sum() * 255) / (177 / 3);
                int sum = (int)(interpretedValues.Sum() * 2550) / (177 / 3);
                int tempSum = sum;

                while (sum > 0)
                {
                    if (sum - 255 >= 0)
                    {
                        values.Add(255);
                        sum -= 255;
                    }
                    else 
                    {
                        values.Add((byte)sum);
                        sum -= sum;
                    }
                }

                int neededAmount = (sum >= 30000) ? 1000 : 5000;
                //int neededAmount = 5000;
                bool suddenChange = (tempSum - PreviousSum) >= neededAmount ? true : false;
                PreviousSum = tempSum;

                // send data to LED strip
                if (values.Count != 0) 
                {
                    LedAPI.RealTime(values.ToArray(), suddenChange);
                }
                else
                    LedAPI.RealTime(new byte[0], false);
            };
            CurrentCapture.StartRecording();
        }

        /// <summary>
        /// Interprets bytes from sound device as integers.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private float[] InterpretBytesAsFloat(byte[] bytes)
        {
            float[] floats = new float[bytes.Length / 4];

            var buffer = new WaveBuffer(bytes);

            // interpret as 32 bit floating point audio
            for (int index = 0; index < bytes.Length / 4; index++)
            {
                //if (index >= (int)Math.Pow(2, 14))
                    //continue;

                var sample = buffer.FloatBuffer[index];

                // absolute value 
                if (sample < 0) sample = -sample;
                //int scaledValue = (int)(sample * 2550000);
                floats[index] = sample;
            }

            return floats;
        }

        private float[] CondenseArray(float[] arr1, int arr2Length)
        {
            int nextIntIndex = (arr1.Length / arr2Length) + 1;
            float[] arr2 = new float[arr2Length];

            float sum = 0;
            int arr2Index = 0;
            for (int i = 0; i < arr1.Length; i++)
            {
                sum += arr1[i];

                // if we need to go to next index in condensedArray
                if (i % nextIntIndex == 0 && i > 0)
                {
                    var avgValue = sum / nextIntIndex;

                    arr2[arr2Index] = avgValue;

                    arr2Index++;
                    sum = 0;
                }
            }

            return arr2;
        }
    }
}
