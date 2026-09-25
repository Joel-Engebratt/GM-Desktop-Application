namespace GM.Development.Mcp
{
    public static class SessionFiles
    {
        // Only copies build files. Existing user data and links are never followed.
        public static void CopyBuild(string source, string destination)
        {
            var directory = new DirectoryInfo(source);
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("A build directory must not be a symbolic link or junction.");
            Directory.CreateDirectory(destination);
            foreach (var entry in directory.EnumerateFileSystemInfos())
            {
                if (entry.Name.Equals("Data", StringComparison.OrdinalIgnoreCase)) continue;
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Refusing to copy a build link: {entry.Name}");
                var target = Path.Combine(destination, entry.Name);
                if (entry is DirectoryInfo child) CopyBuild(child.FullName, target);
                else File.Copy(entry.FullName, target, overwrite: false);
            }
        }
    }
}
