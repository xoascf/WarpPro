using NAudio.Wave;

namespace Beat_Editor
{
	public class WaveformDataProviderLPF : WaveformDataProvider
	{
		public override void readData(WaveStream input)
		{
			base.readData(new LPFWaveStream(input));
		}
	}
}
