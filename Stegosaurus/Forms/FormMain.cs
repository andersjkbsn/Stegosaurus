﻿using Stegosaurus.Algorithm;
using Stegosaurus.Carrier;
using Stegosaurus.Exceptions;
using Stegosaurus.Utility;
using Stegosaurus.Utility.Extensions;
using Stegosaurus.Utility.InputTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Stegosaurus.Forms
{
    public partial class FormMain : Form
    {
        StegoMessage stegoMessage = new StegoMessage();
        ICarrierMedia carrierMedia = null;
        IStegoAlgorithm algorithm = null;

        public FormMain()
        {
            InitializeComponent();
            comboBoxAlgorithmSelection.SelectedIndex = 0;
            algorithm = (IStegoAlgorithm)comboBoxAlgorithmSelection.SelectedItem;
        }

        private void MessageContentFilesListView_DragDrop(object sender, DragEventArgs e)
        {
            string[] inputFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

            IInputType inputContent;
            foreach (string filePath in inputFiles)
            {
                inputContent = new ContentType(filePath);
                InputHelper(inputContent);
            }

            listViewMessageContentFiles.BackColor = Color.White;
        }

        private void MessageContentFilesListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                listViewMessageContentFiles.BackColor = Color.LightGreen;
            }
            else
            {
                e.Effect = DragDropEffects.None;
                listViewMessageContentFiles.BackColor = Color.Red;
            }
        }

        private void MessageContentFilesListView_DragLeave(object sender, EventArgs e)
        {
            listViewMessageContentFiles.BackColor = Color.White;
        }
        

        private void TextMessageTextbox_TextChanged(object sender, EventArgs e)
        {
            stegoMessage.TextMessage = textBoxTextMessage.Text;
            updateCapacityBar();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(stegoMessage.TextMessage);
        }

        private void EmbedButton_Click(object sender, EventArgs e)
        {
            if (carrierMedia == null)
            {
                MessageBox.Show("You must supply a carrier media.");
            }
            else
            {
                if (stegoMessage.InputFiles.Count == 0 && string.IsNullOrEmpty(stegoMessage.TextMessage))
                {
                    algorithm.CarrierMedia = carrierMedia;
                    algorithm.Key = Encoding.UTF8.GetBytes(textBoxEncryptionKey.Text);
                    stegoMessage = algorithm.Extract();
                    if (stegoMessage.InputFiles.Count != 0)
                    {
                        ListViewItem fileItem;
                        foreach (InputFile file in stegoMessage.InputFiles)
                        {
                            fileItem = new ListViewItem(file.Name);
                            fileItem.SubItems.Add(FileSizeExtensions.StringFormatBytes(file.Content.LongLength));
                            fileItem.ImageKey = file.Name.Substring(file.Name.LastIndexOf('.'));
                            if (!imageListIcons.Images.ContainsKey(fileItem.ImageKey))
                                imageListIcons.Images.Add(fileItem.ImageKey, IconExtractor.ExtractIcon(fileItem.ImageKey));
                            
                            listViewMessageContentFiles.Items.Add(fileItem);
                        }
                    }
                    textBoxTextMessage.Text = stegoMessage.TextMessage;
                }
                else
                {
                    try
                    {
                        algorithm.CarrierMedia = carrierMedia;
                        algorithm.Key = Encoding.UTF8.GetBytes(textBoxEncryptionKey.Text);
                        algorithm.Embed(stegoMessage);
                        algorithm.CarrierMedia.SaveToFile("new.png");
                    }
                    catch (StegoAlgorithmException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }

        }

        private void inputBrowseButton_Click(object sender, EventArgs e)
        {
            DialogResult result = InputBrowseDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                IInputType inputContent;
                foreach (string fileName in InputBrowseDialog.FileNames)
                {
                    inputContent = new ContentType(fileName);
                    InputHelper(inputContent);
                }
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            
        }

        private void CarrierMediaPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void CarrierMediaPanel_DragLeave(object sender, EventArgs e)
        {

        }

        private void CarrierMediaPanel_DragDrop(object sender, DragEventArgs e)
        {
            string[] inputFile = (string[]) e.Data.GetData(DataFormats.FileDrop);
            try
            {
                IInputType inputContent = new CarrierType(inputFile[0]);
                InputHelper(inputContent);
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (InvalidWaveFileException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("Invalid file type. Carrier must be picture or .wav audio file.");
            }
            
        }

        private void InputHelper(IInputType _input)
        {
            InputFile inputFile = new InputFile(_input.FilePath);
            FileInfo fileInfo = new FileInfo(_input.FilePath);

            if (_input is ContentType)
            {
                ListViewItem fileItem = new ListViewItem(inputFile.Name);
                fileItem.SubItems.Add(FileSizeExtensions.StringFormatBytes(fileInfo.Length));
                fileItem.ImageKey = fileInfo.Extension;
                if (!imageListIcons.Images.ContainsKey(fileItem.ImageKey))
                    imageListIcons.Images.Add(fileItem.ImageKey, Icon.ExtractAssociatedIcon(_input.FilePath));
                
                stegoMessage.InputFiles.Add(inputFile);
                listViewMessageContentFiles.Items.Add(fileItem);
            }
            else if (_input is CarrierType)
            {
                if (fileInfo.Extension == ".wav")
                {
                    carrierMedia = new AudioCarrier(_input.FilePath);
                    pictureBoxCarrier.Image = Icon.ExtractAssociatedIcon(_input.FilePath).ToBitmap();
                }
                else
                {
                    carrierMedia = new ImageCarrier(_input.FilePath);
                    pictureBoxCarrier.Image = Image.FromFile(fileInfo.FullName);
                }
            }
            updateCapacityBar();
            
        }

        private void AlgorithmSelectionCombobox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            algorithm = (IStegoAlgorithm) comboBoxAlgorithmSelection.SelectedItem;
            algorithm.CarrierMedia = carrierMedia;
            updateCapacityBar();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int[] fileIndices = getSelectedContentIndices();
            int len = fileIndices.Length;
            DialogResult result;
            if (len == 0)
            {
                MessageBox.Show("You must have items selected to save.", "Save Error");
            }
            if (len == 1)
            {
                saveFileDialog.FileName = stegoMessage.InputFiles[fileIndices[0]].Name;
                result = saveFileDialog.ShowDialog();
                if(result == DialogResult.OK)
                {
                    if (saveFileDialog.FileName == "")
                    {
                        MessageBox.Show("The chosen destination cannot be blank.", "Save Error");
                    }
                    else
                    {
                        stegoMessage.InputFiles[fileIndices[0]].SaveTo(saveFileDialog.FileName);
                    }
                }
            }
            else
            {
                result = folderBrowserDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    if (folderBrowserDialog.SelectedPath == "")
                    {
                        MessageBox.Show("The chosen destination cannot be blank.", "Save Error");
                    }
                    else
                    {
                        foreach (int index in fileIndices)
                        {
                            stegoMessage.InputFiles[fileIndices[index]].SaveTo($"{folderBrowserDialog.SelectedPath}\\{stegoMessage.InputFiles[fileIndices[index]].Name}");
                        }
                    }
                }

            }


        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int[] fileIndices = getSelectedContentIndices();
            if (fileIndices.Length == 0)
            {
                MessageBox.Show("You must have items selected to delete.", "Delete Error");
            }
            else
            {
                for (int index = fileIndices.Length - 1; index >= 0; index--)
                {
                    stegoMessage.InputFiles.RemoveAt(fileIndices[index]);
                    listViewMessageContentFiles.Items.RemoveAt(fileIndices[index]);
                }
                updateCapacityBar();
            }
        }

        private int[] getSelectedContentIndices()
        {
            int[] indices = new int[listViewMessageContentFiles.SelectedIndices.Count];
            listViewMessageContentFiles.SelectedIndices.CopyTo(indices, 0);
            return indices;
        }

        private void updateCapacityBar()
        {
            long capacity = 0, size;
            decimal ratio, max = (decimal)progressBarCapacity.Maximum;
            size = stegoMessage.GetCompressedSize();
            if (carrierMedia != null)
            {
                algorithm.CarrierMedia = carrierMedia;
                capacity = algorithm.ComputeBandwidth();
                MessageBox.Show($"{capacity}");
                if (capacity >= size)
                {
                    ratio = 100 * ((decimal)size / capacity);
                }
                else
                {
                    ratio = max;
                }
            }
            else
            {
                ratio = max;
            }
            if (ratio == max)
            {
                labelCapacityWarning.Text = $"< 100%";
                labelCapacityWarning.ForeColor = Color.Red;
            }
            else
            {

                labelCapacityWarning.Text = $"{ratio :.##}%";
                labelCapacityWarning.ForeColor = SystemColors.ControlText;
            }
            progressBarCapacity.Value = (int) ratio;
        }
        
    }
}
