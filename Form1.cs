using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using Nett;
using NAudio.CoreAudioApi;

namespace FlexASIOGUI
{
    public partial class Form1 : Form
    {

        private readonly bool _initDone;
        private readonly string _tomlPath;
        private FlexGuiConfig _flexGuiConfig;
        private const string FlexasioGuiVersion = "EIMChanged";
        private const string FlexasioVersion = "1.9";
        private const string TomlName = "FlexASIO.toml";
        private const string DocUrl = "https://github.com/EchoInMirror/FlexASIO_GUI";

        public Form1()
        {
            InitializeComponent();
            
            Text = $@"FlexASIO GUI {FlexasioGuiVersion}";

            var customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // get the value of the "Language for non-Unicode programs" setting (1252 for English)
            // note: in Win11 this could be UTF-8 already, since it's natively supported
            Encoding.GetEncoding("GBK");

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            CultureInfo.DefaultThreadCurrentCulture = customCulture;
            CultureInfo.DefaultThreadCurrentUICulture = customCulture;

            _tomlPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\{TomlName}";
            
            this.LoadFlexAsioConfig(_tomlPath);

            _initDone = true;
            
            SetStatusMessage($"FlexASIO GUI for FlexASIO {FlexasioVersion} started (NAudio 2.1.0)");
            GenerateOutput();
        }

        public sealed override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        private void LoadFlexAsioConfig(string tomlPath)
        {
            _flexGuiConfig = new FlexGuiConfig();
            if (File.Exists(tomlPath))
            {
                _flexGuiConfig = Toml.ReadFile<FlexGuiConfig>(tomlPath);
            }

            numericBufferSize.Maximum = 8192;
            numericBufferSize.Increment = 16;

            numericLatencyInput.Increment = 0.1m;
            numericLatencyOutput.Increment = 0.1m;

            comboBackend.Items.Add("Windows WASAPI");
            // for (var i = 0; i < Configuration.HostApiCount; i++)
            // {
            //     comboBackend.Items.Add(Configuration.GetHostApiInfo(i).name);
            // }

            comboBackend.SelectedIndex = comboBackend.Items.Contains(_flexGuiConfig.Backend) ? comboBackend.Items.IndexOf(_flexGuiConfig.Backend) : 0;

            if (_flexGuiConfig.BufferSizeSamples != null)
                numericBufferSize.Value = (Int64)_flexGuiConfig.BufferSizeSamples;
            checkBoxSetBufferSize.Checked = numericBufferSize.Enabled = _flexGuiConfig.BufferSizeSamples != null;

            treeDevicesInput.SelectedNode = treeDevicesInput.Nodes.Cast<TreeNode>().FirstOrDefault(x => x.Text == (_flexGuiConfig.Input.Device == "" ? "(None)" : _flexGuiConfig.Input.Device));
            treeDevicesOutput.SelectedNode = treeDevicesOutput.Nodes.Cast<TreeNode>().FirstOrDefault(x => x.Text == (_flexGuiConfig.Output.Device == "" ? "(None)" : _flexGuiConfig.Output.Device));

            checkBoxSetInputLatency.Checked = numericLatencyInput.Enabled = _flexGuiConfig.Input.SuggestedLatencySeconds != null;
            checkBoxSetOutputLatency.Checked = numericLatencyOutput.Enabled = _flexGuiConfig.Output.SuggestedLatencySeconds != null;

            if (_flexGuiConfig.Input.SuggestedLatencySeconds != null)
                numericLatencyInput.Value = (decimal)(double)_flexGuiConfig.Input.SuggestedLatencySeconds;
            if (_flexGuiConfig.Output.SuggestedLatencySeconds != null)
                numericLatencyOutput.Value = (decimal)(double)_flexGuiConfig.Output.SuggestedLatencySeconds;

            numericChannelsInput.Value = _flexGuiConfig.Input.Channels ?? 0;
            numericChannelsOutput.Value = _flexGuiConfig.Output.Channels ?? 0;

            checkBoxWasapiInputSet.Checked = _flexGuiConfig.Input.WasapiExclusiveMode != null || _flexGuiConfig.Input.WasapiAutoConvert != null;
            checkBoxWasapiOutputSet.Checked = _flexGuiConfig.Output.WasapiExclusiveMode != null || _flexGuiConfig.Output.WasapiAutoConvert != null;

            wasapiExclusiveInput.Enabled = _flexGuiConfig.Input.WasapiExclusiveMode != null;
            wasapiExclusiveInput.Checked = _flexGuiConfig.Input.WasapiExclusiveMode ?? false;
            wasapiExclusiveOutput.Enabled = _flexGuiConfig.Output.WasapiExclusiveMode != null;
            wasapiExclusiveOutput.Checked = _flexGuiConfig.Output.WasapiExclusiveMode ?? false;

            wasapiAutoConvertInput.Enabled = _flexGuiConfig.Input.WasapiAutoConvert != null;
            wasapiAutoConvertInput.Checked = _flexGuiConfig.Input.WasapiAutoConvert ?? false;
            wasapiAutoConvertOutput.Enabled = _flexGuiConfig.Output.WasapiAutoConvert != null;
            wasapiAutoConvertOutput.Checked = _flexGuiConfig.Output.WasapiAutoConvert ?? false;
        }

        private static TreeNode[] GetDevices(bool input)
        {
            var treeNodes = new List<TreeNode> { new TreeNode("(None)") };

            var devices = new MMDeviceEnumerator();
            if (!input)
            {
                var endpoints = devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                treeNodes.AddRange(endpoints.Select(endpoint => new TreeNode(endpoint.FriendlyName)));
            }
            else
            {
                var endpoints = devices.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                treeNodes.AddRange(endpoints.Select(endpoint => new TreeNode(endpoint.FriendlyName)));
            }
            return treeNodes.ToArray();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void comboBackend_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is not ComboBox o) return;
            var selectedBackend = o.SelectedItem as string;
            RefreshDevices(selectedBackend);
            if (selectedBackend == "(None)") selectedBackend = "";
            _flexGuiConfig.Backend = selectedBackend;
            GenerateOutput();
        }

        private void RefreshDevices(string selectedBackend)
        {
            var tmpInput = treeDevicesInput.SelectedNode;
            var tmpOutput = treeDevicesOutput.SelectedNode;
            if (selectedBackend == null) return;
            treeDevicesInput.Nodes.Clear();
            treeDevicesInput.Nodes.AddRange(GetDevices(true));
            for (var i = 0; i < treeDevicesInput!.Nodes.Count; i++)
            {
                if (treeDevicesInput?.Nodes[i].Text != tmpInput?.Text) continue;
                treeDevicesInput!.SelectedNode = treeDevicesInput.Nodes[i];
                break;
            }

            treeDevicesOutput.Nodes.Clear();
            treeDevicesOutput.Nodes.AddRange(GetDevices(false));
            for (var i = 0; i < treeDevicesOutput!.Nodes.Count; i++)
            {
                if (treeDevicesOutput?.Nodes[i].Text != tmpOutput?.Text) continue;
                treeDevicesOutput!.SelectedNode = treeDevicesOutput.Nodes[i];
                break;
            }
        }

        private void GenerateOutput()
        {
            if (!_initDone) return;

            if (!checkBoxSetBufferSize.Checked) _flexGuiConfig.BufferSizeSamples = null;
            if (!checkBoxSetInputLatency.Checked) _flexGuiConfig.Input.SuggestedLatencySeconds = null;
            if (!checkBoxSetOutputLatency.Checked) _flexGuiConfig.Output.SuggestedLatencySeconds = null;
            if (!checkBoxWasapiInputSet.Checked)
            {
                _flexGuiConfig.Input.WasapiAutoConvert = null;
                _flexGuiConfig.Input.WasapiExclusiveMode = null;
            }
            if (!checkBoxWasapiOutputSet.Checked)
            {
                _flexGuiConfig.Output.WasapiAutoConvert = null;
                _flexGuiConfig.Output.WasapiExclusiveMode = null;
            }

            configOutput.Clear();
            configOutput.Text = Toml.WriteString(_flexGuiConfig);
        }



        private void SetStatusMessage(string msg)
        {
            toolStripStatusLabel1.Text = $@"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {msg}";
        }

        private void btClipboard_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(configOutput.Text);
            SetStatusMessage("Configuration copied to Clipboard");
        }

        private void btSaveToProfile_Click(object sender, EventArgs e)
        {
            File.WriteAllText(_tomlPath, configOutput.Text);
            SetStatusMessage($"Configuration written to {_tomlPath}");
        }

        private void btSaveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            saveFileDialog.FileName = TomlName;
            var ret = saveFileDialog.ShowDialog();
            if (ret == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, configOutput.Text);
            }
            SetStatusMessage($"Configuration written to {saveFileDialog.FileName}");
        }

         private void treeDevicesInput_AfterSelect(object sender, TreeViewEventArgs e)
         {
             if (sender == null) return;
             e.Node!.Checked = true;
             OnTreeViewSelected(eventArgs: e, forInput: true);
         }

        private void treeDevicesOutput_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (sender == null) return;
            e.Node!.Checked = true;
            OnTreeViewSelected(eventArgs: e, forInput: false);
        }

        private void treeDevicesOutput_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (sender == null) return;
            OnTreeViewSelected(eventArgs: e, forInput: false);
        }

        private void treeDevicesInput_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (sender == null) return;
            OnTreeViewSelected(eventArgs: e, forInput: true);
        }

        private static void UnCheckAllOthers(TreeNode treeNode)
        {
            foreach (TreeNode node in treeNode.TreeView.Nodes)
            {
                if (node != treeNode)
                {
                    node.Checked = false;
                }
            }
        }

        private void OnTreeViewSelected(TreeViewEventArgs eventArgs, bool forInput)
        {
            if (eventArgs.Node!.Checked != true) return;
            if (forInput)
                _flexGuiConfig.Input.Device = eventArgs.Node.Text == @"(None)" ? "" : eventArgs.Node.Text;
            else
                _flexGuiConfig.Output.Device = eventArgs.Node.Text == @"(None)" ? "" : eventArgs.Node.Text;
            UnCheckAllOthers(eventArgs.Node);
            GenerateOutput();
        }

        private void numericChannelsOutput_ValueChanged(object sender, EventArgs e)
        {
            var o = sender as NumericUpDown;
            if (o == null) return;
            _flexGuiConfig.Output.Channels = (o.Value > 0 ? (int?)o.Value : null);
            GenerateOutput();
        }

        private void numericChannelsInput_ValueChanged(object sender, EventArgs e)
        {
            var o = sender as NumericUpDown;
            if (o == null) return;
            _flexGuiConfig.Input.Channels = (o.Value > 0 ? (int?)o.Value : null);
            GenerateOutput();
        }

        private void numericLatencyOutput_ValueChanged(object sender, EventArgs e)
        {
            var o = sender as NumericUpDown;
            if (o == null) return;
            if (checkBoxSetOutputLatency.Enabled)
            {
                _flexGuiConfig.Output.SuggestedLatencySeconds = (o.Value > 0 ? (double)o.Value : 0);
                GenerateOutput();
            }
        }

        private void numericLatencyInput_ValueChanged(object sender, EventArgs e)
        {
            var o = sender as NumericUpDown;
            if (o == null) return;
            if (checkBoxSetInputLatency.Enabled)
            {
                _flexGuiConfig.Input.SuggestedLatencySeconds = (o.Value > 0 ? (double)o.Value : 0);
                GenerateOutput();
            }
        }

        private void wasapiAutoConvertOutput_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            _flexGuiConfig.Output.WasapiAutoConvert = o.Checked;
            GenerateOutput();
        }

        private void wasapiExclusiveOutput_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            _flexGuiConfig.Output.WasapiExclusiveMode = o.Checked;
            GenerateOutput();
        }

        private void wasapiAutoConvertInput_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            _flexGuiConfig.Input.WasapiAutoConvert = o.Checked;
            GenerateOutput();
        }

        private void wasapiExclusiveInput_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            _flexGuiConfig.Input.WasapiExclusiveMode = o.Checked;
            GenerateOutput();
        }

        private void numericBufferSize_ValueChanged(object sender, EventArgs e)
        {
            var o = sender as NumericUpDown;
            if (o == null) return;
            _flexGuiConfig.BufferSizeSamples = (o.Value > 0 ? (int)o.Value : 0);
            GenerateOutput();
        }

 
        private void checkBoxSetInputLatency_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            numericLatencyInput.Enabled = o.Checked;
            if (o.Checked == false)
            {
                _flexGuiConfig.Input.SuggestedLatencySeconds = null;
            }
            else
            {
                numericLatencyInput_ValueChanged(numericLatencyInput, null);
            }
            GenerateOutput();
        }

        private void checkBoxSetOutputLatency_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            numericLatencyOutput.Enabled = o.Checked;
            if (o.Checked == false) {
                _flexGuiConfig.Output.SuggestedLatencySeconds = null;
            }
            else
            {
                numericLatencyOutput_ValueChanged(numericLatencyOutput, null);
            }
            GenerateOutput();
        }
       

        private void checkBoxSetBufferSize_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            numericBufferSize.Enabled = o.Checked;
            if (o.Checked == false) { 
                _flexGuiConfig.BufferSizeSamples = null; 
            }
            else
            {
                numericBufferSize_ValueChanged(numericBufferSize, null);
            }
            GenerateOutput();
        }

        private void checkBoxWasapiInputSet_CheckedChanged(object sender, EventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null) return;
            wasapiAutoConvertInput.Enabled = o.Checked;
            wasapiExclusiveInput.Enabled = o.Checked;

            if (o.Checked == false)
            {
                _flexGuiConfig.Input.WasapiAutoConvert = null;
                _flexGuiConfig.Input.WasapiExclusiveMode = null;
            }
            else
            {
                _flexGuiConfig.Input.WasapiAutoConvert = wasapiAutoConvertInput.Checked;
                _flexGuiConfig.Input.WasapiExclusiveMode = wasapiExclusiveInput.Checked;
            }
            GenerateOutput();
        }

        private void checkBoxWasapiOutputSet_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is not CheckBox o) return;
            wasapiAutoConvertOutput.Enabled = o.Checked;
            wasapiExclusiveOutput.Enabled = o.Checked;

            if (o.Checked == false)
            {
                _flexGuiConfig.Output.WasapiAutoConvert = null;
                _flexGuiConfig.Output.WasapiExclusiveMode = null;
            }
            else
            {
                _flexGuiConfig.Output.WasapiAutoConvert = wasapiAutoConvertOutput.Checked;
                _flexGuiConfig.Output.WasapiExclusiveMode = wasapiExclusiveOutput.Checked;
            }
            GenerateOutput();
        }

        private void btRefreshDevices_Click(object sender, EventArgs e)
        {
            var selectedBackend = comboBackend.SelectedItem as string;
            RefreshDevices(selectedBackend);
        }

        private void linkLabelDocs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(DocUrl) { UseShellExecute = true });
        }

        private void btLoadFrom_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            openFileDialog.FileName = TomlName;
            openFileDialog.Filter = @"FlexASIO Config (*.toml)|*.toml";
            openFileDialog.CheckFileExists = true;
            var ret = openFileDialog.ShowDialog();
            if (ret == DialogResult.OK)
            {
                try
                {
                    this.LoadFlexAsioConfig(openFileDialog.FileName);
                }
                catch (Exception)
                {
                    SetStatusMessage($"Error loading from {openFileDialog.FileName}");
                    this.LoadFlexAsioConfig(_tomlPath);
                    return;
                }
                
            }
            SetStatusMessage($"Configuration loaded from {openFileDialog.FileName}");
        }
    }
}
