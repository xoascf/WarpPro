using System;
using System.ComponentModel;
using System.IO;
using Beat_Editor.Controller;
using NAudio.Lame;
using NAudio.Wave;

namespace Beat_Editor
{
	public class FileProcessor : IDisposable
	{
		public Action<int> ProgressNotify;

		public Action<string> Finished;

		public WaveDspStream dspStream;

		public WaveStream input;

		private Stream audioFileWriter;

		private BackgroundWorker bw;

		private string outFileName;

		private static readonly int BUFFSIZE = 16384;

		private bool isDisposed;

		public FileProcessor(string inputFileName)
		{
			WaveStream waveStream = OpenAudioReaderStream(inputFileName);
			if (waveStream == null)
			{
				throw new Exception("Unsupported file type");
			}
			WaveChannel32 waveChannel = new WaveChannel32(waveStream)
			{
				PadWithZeroes = false
			};
			dspStream = new WaveDspStream(waveChannel, LicenseController.Instance.IsTrialExpired);
			input = dspStream;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					dspStream.Dispose();
					input.Dispose();
					audioFileWriter.Dispose();
				}
				isDisposed = true;
			}
		}

		public static WaveStream OpenAudioReaderStream(string filename)
		{
			string text = Path.GetExtension(filename).ToLower();
			if (text == ".mp3")
			{
				return new Mp3FileReader(filename);
			}
			if (text == ".wav")
			{
				return new WaveFileReader(filename);
			}
			return null;
		}

		private void bw_DoWork(object sender, DoWorkEventArgs e)
		{
			byte[] buffer = new byte[BUFFSIZE];
			long num = 0L;
			long length = input.Length;
			int num2 = 0;
			string obj = "";
			bw.ReportProgress(0);
			try
			{
				int num3;
				while ((num3 = input.Read(buffer, 0, BUFFSIZE)) != 0)
				{
					audioFileWriter.Write(buffer, 0, num3);
					num += num3;
					int num4 = (int)(100L * num / length);
					if (num4 > num2)
					{
						num2 = num4;
						bw.ReportProgress(num2);
					}
					if (bw.CancellationPending)
					{
						break;
					}
				}
				audioFileWriter.Close();
			}
			catch (Exception ex)
			{
				obj = "error: " + ex;
			}
			bw.ReportProgress(101);
			if (bw.CancellationPending)
			{
				File.Delete(outFileName);
				obj = "save canceled";
			}
			if (Finished != null)
			{
				Finished(obj);
			}
		}

		public void Cancel()
		{
			bw.CancelAsync();
		}

		private void notifyProxy(object sender, ProgressChangedEventArgs e)
		{
			if (ProgressNotify != null)
			{
				ProgressNotify(e.ProgressPercentage);
			}
		}

		private void Completed(object sender, RunWorkerCompletedEventArgs e)
		{
			audioFileWriter = null;
			input = null;
			bw = null;
			LicenseController.Instance.IncSaveCount();
		}

		public bool Process(string outputFileName, int bitrate, ID3TagData id3 = null)
		{
			string extension = Path.GetExtension(outputFileName);
			if (extension == ".mp3")
			{
				audioFileWriter = new LameMP3FileWriter(outputFileName, input.WaveFormat, bitrate, id3);
			}
			else
			{
				if (!(extension == ".wav"))
				{
					throw new Exception("Illegal file extension");
				}
				input = new Wave32To16Stream(input);
				audioFileWriter = new WaveFileWriter(outputFileName, input.WaveFormat);
			}
			outFileName = outputFileName;
			bw = new BackgroundWorker
			{
				WorkerReportsProgress = true,
				WorkerSupportsCancellation = true
			};
			bw.ProgressChanged += notifyProxy;
			bw.DoWork += bw_DoWork;
			bw.RunWorkerCompleted += Completed;
			bw.RunWorkerAsync();
			return true;
		}
	}
}
