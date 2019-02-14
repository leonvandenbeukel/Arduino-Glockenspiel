/*
 * MIT License
 * 
 * Copyright (c) 2019 Leon van den Beukel
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * Source: 
 * https://github.com/leonvandenbeukel/Arduino-Glockenspiel
 *
 */

using System;
using System.IO;
using System.Text;
using Android.App;
using Android.Bluetooth;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Util;

namespace GlockenspielApp
{
    public enum Notes
    {
        R = 0,
        C = 1,
        D = 2,
        E = 4,
        F = 8,
        G = 16,
        A = 32,
        B = 64,
        C2 = 128
    }

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private readonly string _tag = $"{Application.Context.PackageName} {nameof(MainActivity)}";
        private Button _buttonConnect;
        private Button _buttonC;
        private Button _buttonD;
        private Button _buttonE;
        private Button _buttonF;
        private Button _buttonG;
        private Button _buttonA;
        private Button _buttonB;
        private Button _buttonC2;
        private Button _buttonR;
        private Button _buttonCombine;
        private Button _buttonDemoSong1; 
        private Button _buttonDemoSong2;
        private Button _buttonDemoSong3;
        private Button _buttonClear;
        private Button _buttonStopPlaying;
        private Button _buttonUpload;
        private EditText _editTextBtDevice;
        private EditText _editTextMelody;
        private EditText _editTextBpm;
        private TextView _textViewConnecting;
        private BluetoothAdapter _bluetoothAdapter;
        private BluetoothSocket _btSocket;
        private Stream _outStream;
        private bool _connected;

        private static readonly UUID MyUuid = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            FindViews();
            SetHandlers();
        }

        private void FindViews()
        {
            _buttonConnect = FindViewById<Button>(Resource.Id.buttonConnect);
            _buttonC = FindViewById<Button>(Resource.Id.buttonC);
            _buttonD = FindViewById<Button>(Resource.Id.buttonD);
            _buttonE = FindViewById<Button>(Resource.Id.buttonE);
            _buttonF = FindViewById<Button>(Resource.Id.buttonF);
            _buttonG = FindViewById<Button>(Resource.Id.buttonG);
            _buttonA = FindViewById<Button>(Resource.Id.buttonA);
            _buttonB = FindViewById<Button>(Resource.Id.buttonB);
            _buttonC2 = FindViewById<Button>(Resource.Id.buttonC2);
            _buttonR = FindViewById<Button>(Resource.Id.buttonR);
            _buttonDemoSong1 = FindViewById<Button>(Resource.Id.buttonDemoSong1);
            _buttonDemoSong2 = FindViewById<Button>(Resource.Id.buttonDemoSong2);
            _buttonDemoSong3 = FindViewById<Button>(Resource.Id.buttonDemoSong3);
            _buttonUpload = FindViewById<Button>(Resource.Id.buttonUpload);
            _buttonClear = FindViewById<Button>(Resource.Id.buttonClear);
            _buttonStopPlaying = FindViewById<Button>(Resource.Id.buttonStop);
            _buttonCombine = FindViewById<Button>(Resource.Id.buttonCombine);
            _editTextBtDevice = FindViewById<EditText>(Resource.Id.editTextBTDevice);
            _editTextMelody = FindViewById<EditText>(Resource.Id.editTextMelody);
            _editTextBpm = FindViewById<EditText>(Resource.Id.editTextBPM);
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            _textViewConnecting = FindViewById<TextView>(Resource.Id.textViewConnecting);
            _textViewConnecting.Visibility = ViewStates.Invisible;
        }

        private void SetHandlers()
        {
            _buttonConnect.Click += _buttonConnect_ClickAsync;
            _buttonC.Click += (sender, e) => AddNote("C");
            _buttonD.Click += (sender, e) => AddNote("D");
            _buttonE.Click += (sender, e) => AddNote("E");
            _buttonF.Click += (sender, e) => AddNote("F");
            _buttonG.Click += (sender, e) => AddNote("G");
            _buttonA.Click += (sender, e) => AddNote("A");
            _buttonB.Click += (sender, e) => AddNote("B");
            _buttonC2.Click += (sender, e) => AddNote("C2");
            _buttonR.Click += (sender, e) => AddNote("R");
            _buttonCombine.Click += (sender, e) => AddNote("+");
            _buttonDemoSong1.Click += (sender, e) =>
            {
                _editTextMelody.Text = "G,E,E,D,E,G,G,R,A,A,C2,A,A,G,G,R,G,E,E,D,E,G,G,R,A,A,G,C,E,D,C,R,R";
                _editTextBpm.Text = "84";
            };
            _buttonDemoSong2.Click += (sender, e) =>
            {
                _editTextMelody.Text = "G,E,D,C,D,E,G,E,D,C,D,E,G,E,G,A,E,A,G,E,D,C,R,R";
                _editTextBpm.Text = "120";
            };
            _buttonDemoSong3.Click += (sender, e) =>
            {
                _editTextMelody.Text = "C+E,R,F+D,F+D,R,E+C,E+C,D,F+D,F+D,R,E+C,E+C,D,F+D,F+D,R,E+C,E+C,R,R,R,E+G,R,F+A,F+A,R,B+G,B+G,R,C2+A,C2+A,R,B+G,B+G,R,A+F,A+F,R,G+E,C+E+G+C2,R,R,R";
                _editTextBpm.Text = "150";
            };
            _buttonClear.Click += (sender, e) => _editTextMelody.Text = string.Empty;
            _buttonStopPlaying.Click += (sender, e) => WriteData($"{_editTextBpm.Text},0|");
            _buttonUpload.Click += _buttonUpload_Click;
        }

        private void AddNote(string note)
        {
            _editTextMelody.Text += $"{(_editTextMelody.Text.Length == 0 || note == "+" || _editTextMelody.Text.Substring(_editTextMelody.Text.Length - 1, 1) == "+" ? "" : ",")}{note}";
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            _buttonUpload_Click(null, null);

            Toast.MakeText(this, "Uploaded to Glockenspiel, now playing...", ToastLength.Short).Show();
        }

        private void _buttonUpload_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_editTextMelody.Text))
                return;

            if (!_connected)
            {
                Toast.MakeText(this, "Not connected to any Bluetooth device!", ToastLength.Short).Show();
                return;
            }

            var values = _editTextMelody.Text.Split(',');
            var uploadstring = string.Empty;

            foreach (var val in values)
            {
                if (val.Contains('+'))
                {
                    var combinedNotes = val.Split('+');
                    int totalvalue = 0;

                    for (int i = 0; i < combinedNotes.Length; i++)
                    {
                        if (Enum.TryParse<Notes>(combinedNotes[i], out Notes note))
                            totalvalue += (int)note;
                    }

                    uploadstring += $",{totalvalue}";
                }
                else
                {
                    if (Enum.TryParse<Notes>(val, out Notes note))
                        uploadstring += $",{(int)note}";
                }
            }

            WriteData($"{_editTextBpm.Text},{uploadstring.Substring(1)}|");
        }

        private async void _buttonConnect_ClickAsync(object sender, EventArgs e)
        {
            if (!_bluetoothAdapter.Enable())
            {
                Toast.MakeText(this, "Bluetooth deactivated!", ToastLength.Short).Show();
                _buttonConnect.Enabled = true;
                return;
            }

            _textViewConnecting.Visibility = ViewStates.Visible;
            _buttonConnect.Enabled = false;

            foreach (var item in _bluetoothAdapter.BondedDevices)
            {
                if (item.Name == _editTextBtDevice.Text)
                {
                    var device = _bluetoothAdapter.GetRemoteDevice(item.Address);
                    _bluetoothAdapter.CancelDiscovery();
                    Log.Info(_tag, $"Try to connect to {device.Name}");

                    try
                    {
                        _btSocket = device.CreateRfcommSocketToServiceRecord(MyUuid);                        

                        await _btSocket.ConnectAsync();

                        if (_btSocket.IsConnected)
                        {
                            Toast.MakeText(this, "Connected to bluetooth device!", ToastLength.Short).Show();
                            _buttonConnect.Text = "Connected";
                            _buttonConnect.SetTextColor(Android.Graphics.Color.DarkGreen);
                            _editTextBtDevice.Enabled = false;
                            _connected = true;
                            _textViewConnecting.Visibility = ViewStates.Invisible;
                        }
                    }
                    catch (Exception ex)
                    {
                        _connected = false;
                        _textViewConnecting.Visibility = ViewStates.Invisible;
                        var error = $"Cannot connect to BT device: {ex.Message}, {ex.StackTrace}.";
                        Log.Error(_tag, error);
                        Toast.MakeText(this, "Cannot connect to bluetooth device!", ToastLength.Short).Show();
                    }
                }
            }
        }

        public string WriteData(string data)
        {
            try
            {
                _outStream = _btSocket.OutputStream;
            }
            catch (Exception ex)
            {
                Log.Error(_tag, $"Cannot get OutputStream from BT device: {ex.Message}, {ex.StackTrace}.");
                return $"Cannot connecto to bluetooth device ({ex.Message})!";
            }

            byte[] buffer = Encoding.UTF8.GetBytes(data);

            try
            {
                _outStream.WriteAsync(buffer, 0, buffer.Length);
                return string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(_tag, $"Cannot write data to BT device: {ex.Message}, {ex.StackTrace}.");
                return $"Cannot send data to bluetooth device ({ex.Message})!";
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;

            if (id == Resource.Id.action_disconnect)
            {
                try
                {
                    if (_btSocket.IsConnected)
                    {
                        _btSocket.Close();
                        _btSocket.Dispose();
                        _buttonConnect.Text = "Connect";
                        _buttonConnect.SetTextColor(Android.Graphics.Color.Black);
                        _buttonConnect.Enabled = true;
                        _editTextBtDevice.Enabled = true;
                        Toast.MakeText(this, "Connection closed...", ToastLength.Short).Show();
                    }

                    _connected = false;
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, $"Cannot close: {ex.Message}", ToastLength.Short).Show();
                }

                return true;
            }

            return base.OnOptionsItemSelected(item);
        }
    }
}