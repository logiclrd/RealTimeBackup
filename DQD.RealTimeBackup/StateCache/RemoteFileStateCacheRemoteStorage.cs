using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using DQD.RealTimeBackup.Storage;
using DQD.RealTimeBackup.Utility;

namespace DQD.RealTimeBackup.StateCache
{
	public class RemoteFileStateCacheRemoteStorage : DiagnosticOutputBase, IRemoteFileStateCacheStorage
	{
		OperatingParameters _parameters;

		IRemoteStorage _remoteStorage;

		public RemoteFileStateCacheRemoteStorage(OperatingParameters parameters, IRemoteStorage remoteStorage)
		{
			_parameters = parameters;

			_remoteStorage = remoteStorage;
		}

		public IEnumerable<int> EnumerateBatches()
		{
			var stateFiles = _remoteStorage.EnumerateFiles("/state/", recursive: false).ToList();

			int batchNumber = -1;

			var batchNumbers = stateFiles
				.Select(file => Path.GetFileName(file.Path))
				.Where(fileName => int.TryParse(fileName, out batchNumber))
				.Select(fileName => batchNumber)
				.ToList();

			batchNumbers.Sort();

			return batchNumbers;
		}

		public int GetBatchFileSize(int batchNumber)
		{
			throw new NotSupportedException();
		}

		public StreamWriter OpenBatchFileWriter(int batchNumber)
		{
			throw new NotSupportedException();
		}

		public StreamReader OpenBatchFileReader(int batchNumber)
		{
			return new StreamReader(OpenBatchFileStream(batchNumber));
		}

		public Stream OpenBatchFileStream(int batchNumber)
		{
			string batchFileName = "/state/" + batchNumber;

			var buffer = new MemoryStream();

			_remoteStorage.DownloadFileDirect(batchFileName, buffer, CancellationToken.None);

			buffer.Position = 0;

			return buffer;
		}

		public StreamWriter OpenNewBatchFileWriter(int batchNumber)
		{
			throw new NotSupportedException();
		}

		public void SwitchToConsolidatedFile(IEnumerable<int> mergedBatchNumbersForDeletion, int mergeIntoBatchNumber)
		{
			throw new NotSupportedException();
		}
	}
}
