using Luval.Logging.Providers;
using Microsoft.Extensions.Logging;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.MN.Core
{
    public class AudioFormatConverter
    {

        private string[] _extensions = new[] { ".wav", ".m4a", ".wma", ".mp3" };
        public FileInfo AudioFile { get; private set; }
        public ILogger Logger { get; private set; }

        public AudioFormatConverter(string audioFile, ILogger logger) : this(new FileInfo(audioFile), logger)
        {

        }
        public AudioFormatConverter(FileInfo audioFile, ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            AudioFile = audioFile ?? throw new ArgumentNullException(nameof(audioFile));
            if (!AudioFile.Exists) throw new ArgumentException($"Audio file does not exist");
            if (!_extensions.Contains(audioFile.Extension)) throw new ArgumentException($"Extension {AudioFile.Extension} not supported");
        }


        public string Convert()
        {
            var file = Path.Combine(AudioFile.DirectoryName, AudioFile.Name.Replace(AudioFile.Extension, ".wav"));
            Convert(file);
            return file;
        }

        public void Convert(string destionationFileName)
        {
            if(AudioFile.Extension.ToLowerInvariant() == ".wav")
            {
                Logger.LogInformation($"File {AudioFile.Name} do not require conversion");
                return;
            }
            if (File.Exists(destionationFileName))
            {
                Logger.LogInformation($"File {destionationFileName} already exist, using that instead");
                return;
            }
            Logger.LogInformation($"Converting {AudioFile.Name} to {destionationFileName}");
            try
            {
                using (var reader = new MediaFoundationReader(AudioFile.FullName))
                {
                    WaveFileWriter.CreateWaveFile(destionationFileName, reader);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unable to convert {AudioFile.Name}");
                throw ex;
            }
        }
    }
}
