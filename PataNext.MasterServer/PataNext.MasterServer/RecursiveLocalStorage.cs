using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameHost.Core.IO;
using GameHost.IO;

namespace PataNext.MasterServer
{
	public class RecursiveLocalStorage : IStorage
	{
		public readonly LocalStorage  BoxedStorage;
		public readonly DirectoryInfo Directory;

		public string CurrentPath { get; }

		public RecursiveLocalStorage(IStorage storage)
		{
			if (storage is not LocalStorage localStorage)
				throw new InvalidCastException($"{nameof(storage)} is not a {nameof(LocalStorage)}");

			BoxedStorage = localStorage;
			Directory    = new DirectoryInfo(storage.CurrentPath!);
			CurrentPath  = localStorage.CurrentPath;
		}

		public Task<IEnumerable<IFile>> GetFilesAsync(string pattern)
		{
			return Task.FromResult(Directory.GetFiles(pattern, SearchOption.AllDirectories).Select(f => (IFile) new LocalFile(f)));
		}

		public async Task<IStorage> GetOrCreateDirectoryAsync(string path)
		{
			return new RecursiveLocalStorage(await BoxedStorage.GetOrCreateDirectoryAsync(path));
		}
	}
}