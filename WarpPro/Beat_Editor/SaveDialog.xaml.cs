using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Beat_Editor.Controller;
using Beat_Editor.Model;
using Id3Lib;
using Microsoft.Win32;
using Mp3Lib;

namespace Beat_Editor
{
	public partial class SaveDialog : Window, IComponentConnector
	{
		private string inputname;

		private BeatCollection beats;

		private TagHandler inputTags;

		public string SaveName
		{
			get
			{
				return textBox_SaveName.Text;
			}
			set
			{
				textBox_SaveName.Text = value;
			}
		}

		public SaveDialog(MainWindow owner, string inputFileName, BeatCollection beats)
		{
			InitializeComponent();
			base.Owner = owner;
			this.beats = beats;
			base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			inputname = inputFileName;
			string extension = Path.GetExtension(inputFileName);
			TextBox textBox = textBox_SaveName;
			string[] obj = new string[5]
			{
				Path.GetDirectoryName(inputFileName),
				null,
				null,
				null,
				null
			};
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			obj[1] = directorySeparatorChar.ToString();
			obj[2] = Path.GetFileNameWithoutExtension(inputFileName);
			obj[3] = "-warped";
			obj[4] = extension;
			textBox.Text = string.Concat(obj);
			string text = "";
			if (LicenseController.Instance.LicenseLevel == 0)
			{
				text = string.Format("Trial mode - {0} file save times remaining", LicenseController.Instance.FileSavesRemaining());
			}
			textbox_Trial.Text = text;
			SetExtensionType(extension);
			textBox_BPM.Text = Math.Round(beats.targetBPM, 1).ToString().Replace(",", ".");
			readFileTags(inputFileName);
		}

		private void readFileTags(string inputFileName)
		{
			try
			{
				Mp3File mp3File = new Mp3File(inputFileName);
				inputTags = mp3File.TagHandler;
				textBox_Track.Text = inputTags.Title;
				textBox_Artist.Text = inputTags.Artist;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exp in readFileTags:" + ex);
			}
		}

		private void buttonBrowse_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.FileName = textBox_SaveName.Text;
			saveFileDialog.Filter = "Audio files (*.mp3; *.wav)|*.mp3;*.wav";
			if (saveFileDialog.ShowDialog() != false)
			{
				SaveName = saveFileDialog.FileName;
				SetExtensionType(Path.GetExtension(SaveName));
			}
		}

		private void button_Save_Click(object sender, RoutedEventArgs e)
		{
			SaveProgress saveProgress = null;
			SetExtensionType(comboBox_Encoding.Text);
			try
			{
				if (SaveName.Length < 1 || (File.Exists(SaveName) && MessageBox.Show(string.Format("File {0} already exists.\nDo you want to overwrite the file?", Path.GetFileName(SaveName)), "Confirm overwriting file", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel))
				{
					return;
				}
				int result;
				if (int.TryParse(comboBox_Bitrate.Text, out result) && result >= 32 && result <= 320)
				{
					string text = textBox_BPM.Text;
					if (!double.TryParse(text, out beats.targetBPM))
					{
						double.TryParse(text.Replace(".", ","), out beats.targetBPM);
					}
					FileProcessor fileProcessor = new FileProcessor(inputname);
					beats.setupAudioModification(fileProcessor.dspStream);
					saveProgress = new SaveProgress(this, fileProcessor, SaveName);
					FileProcessor fileProcessor2 = fileProcessor;
					fileProcessor2.ProgressNotify = (Action<int>)Delegate.Combine(fileProcessor2.ProgressNotify, new Action<int>(saveProgress.SetProgress));
					fileProcessor2 = fileProcessor;
					fileProcessor2.Finished = (Action<string>)Delegate.Combine(fileProcessor2.Finished, new Action<string>(handler_SaveFinished));
					if (LicenseController.Instance.isLicenseLastWebUpdateDateValid)
					{
						saveProgress.Show();
						fileProcessor.Process(SaveName, result);
					}
				}
				else
				{
					MessageBox.Show("Invalid bitrate setting " + comboBox_Bitrate.Text);
				}
			}
			catch (Exception ex)
			{
				if (saveProgress != null)
				{
					saveProgress.Close();
				}
				string text2 = string.Format("Error: Can't save file \"{0}\":\n{1}", SaveName, ex.Message);
				MessageBox.Show(text2);
				Console.WriteLine(text2);
			}
			finally
			{
			}
		}

		private void handler_SaveFinished(string err)
		{
#if NET40
			Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
#else
			base.Dispatcher.Invoke(delegate
#endif

			{
				if (err.Length > 0)
				{
					MessageBox.Show(err);
				}
				else
				{
					if (Path.GetExtension(SaveName) == ".mp3" && checkBox_Metadata.IsChecked.Value)
					{
						try
						{
							Mp3File mp3File = new Mp3File(textBox_SaveName.Text);
							if (checkBox_StoreOther.IsChecked.Value)
							{
								mp3File.TagHandler = inputTags;
							}
							TagHandler tagHandler = mp3File.TagHandler;
							tagHandler.Title = textBox_Track.Text;
							tagHandler.Artist = textBox_Artist.Text;
							tagHandler.BPM = textBox_BPM.Text;
							mp3File.Update();
							File.Delete(MakeCustomExtSaveName(".bak"));
							((MainWindow)base.Owner).waveform.SetClean();
						}
						catch (Exception ex)
						{
							Console.WriteLine("Exp in saving tags:" + ex);
							MessageBox.Show("Error in saving Id3 tags to {0}. Please try again.", Path.GetFileName(SaveName));
							return;
						}
					}
					Close();
					Application.Current.MainWindow.Focus();
				}
			});
		}

		private string MakeCustomExtSaveName(string ext)
		{
			string saveName = SaveName;
			string directoryName = Path.GetDirectoryName(saveName);
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			return directoryName + directorySeparatorChar + Path.GetFileNameWithoutExtension(saveName) + ext;
		}

		private void EnableMetaDataEdits(bool enabled)
		{
			if (textBox_Track != null)
			{
				textBox_Track.IsEnabled = enabled;
				textBox_Artist.IsEnabled = enabled;
				textBox_BPM.IsEnabled = enabled;
				checkBox_StoreOther.IsEnabled = enabled;
			}
		}

		private void button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void checkBox_Metadata_Click(object sender, RoutedEventArgs e)
		{
			EnableMetaDataEdits(checkBox_Metadata.IsChecked.Value);
		}

		private void SetExtensionType(string ext)
		{
			ext = ext.Replace(".", "");
			if (ext == "wav")
			{
				checkBox_Metadata.IsEnabled = false;
			}
			else
			{
				checkBox_Metadata.IsEnabled = true;
			}
			comboBox_Bitrate.IsEnabled = checkBox_Metadata.IsEnabled;
			EnableMetaDataEdits(checkBox_Metadata.IsEnabled);
			SaveName = MakeCustomExtSaveName("." + ext);
			comboBox_Encoding.SelectedItem = ext;
		}

		private void comboBox_Encoding_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (checkBox_Metadata != null)
			{
				string extensionType = e.AddedItems[0].ToString();
				SetExtensionType(extensionType);
			}
		}
	}
}
