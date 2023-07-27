namespace TPSBR
{
	using System.IO;
	using System.Text;

	public sealed class LogRecorder
	{
		// CONSTANTS

		public static readonly Encoding DefaultEncoding = Encoding.UTF8;
		public static readonly string   DefaultNewLine  = "\n";

		// PUBLIC MEMBERS

		public bool IsInitialized => _fileStream != null;

		// PRIVATE MEMBERS

		private FileStream _fileStream;

		private readonly Encoding _encoding;
		private readonly byte[]   _newLine;
		private readonly byte[]   _buffer = new byte[65536];

		// CONSTRUCTORS

		public LogRecorder()
		{
			_encoding = DefaultEncoding;
			_newLine  = _encoding.GetBytes(DefaultNewLine);
		}

		public LogRecorder(Encoding encoding, string newLine)
		{
			_encoding = encoding;
			_newLine  = _encoding.GetBytes(newLine);
		}

		// PUBLIC METHODS

		public void Initialize(string name)
		{
			if (_fileStream != null)
				return;

			_fileStream = File.OpenWrite(name);
		}

		public void Deinitialize()
		{
			if (_fileStream == null)
				return;

			_fileStream.Flush(true);
			_fileStream.Close();
			_fileStream = null;
		}

		public void Write(string message)
		{
			if (_fileStream == null)
				return;
			if (string.IsNullOrEmpty(message) == true)
				return;

			int messageCount = _encoding.GetBytes(message, 0, message.Length, _buffer, 0);
			_fileStream.Write(_buffer, 0, messageCount);

			_fileStream.Write(_newLine, 0, _newLine.Length);
		}

		public void Flush()
		{
			if (_fileStream == null)
				return;

			_fileStream.Flush(true);
		}
	}
}
