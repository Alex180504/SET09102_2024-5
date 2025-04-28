using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Mocks
{
    /// <summary>
    /// A mock implementation of Mapsui's BitmapRegistry for unit testing map functionality.
    /// </summary>
    public class MockMapsuiBitmapRegistry
    {
        private readonly Dictionary<int, byte[]> _registeredBitmaps = new Dictionary<int, byte[]>();
        private int _nextId = 1;

        public int Register(Stream stream)
        {
            int id = _nextId++;

            // Store bitmap data if needed
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                _registeredBitmaps[id] = ms.ToArray();
            }

            return id;
        }

        public void UnRegister(int bitmapId)
        {
            if (_registeredBitmaps.ContainsKey(bitmapId))
            {
                _registeredBitmaps.Remove(bitmapId);
            }
        }

        public object Get(int bitmapId)
        {
            // Return null for simplicity
            return null;
        }
    }
}
