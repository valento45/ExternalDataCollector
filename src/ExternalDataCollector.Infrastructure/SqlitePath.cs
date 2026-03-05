using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalDataCollector.Infrastructure
{
    public static class SqlitePath
    {
        public static string GetDefaultDbPath()
        {
            // Windows: C:\Users\<user>\AppData\Local\ExternalDataCollector\data\rates.db
            // Linux: ~/.local/share/ExternalDataCollector/data/rates.db
            // macOS: ~/Library/Application Support/ExternalDataCollector/data/rates.db
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(root, "ExternalDataCollector", "data");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "rates.db");
        }
    }
}
