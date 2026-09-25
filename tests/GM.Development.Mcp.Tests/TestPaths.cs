using System.Reflection;

namespace GM.Development.Mcp.Tests
{
    internal static class TestPaths
    {
        public static string Configuration => typeof(TestPaths).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        public static string RepositoryRoot
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory is not null)
                {
                    if (File.Exists(Path.Combine(directory.FullName, "GM Desktop Application", "GM Desktop Application.slnx")))
                        return directory.FullName;
                    directory = directory.Parent;
                }
                throw new InvalidOperationException("Run MCP tests inside the repository checkout.");
            }
        }
        public static string Server => Path.Combine(RepositoryRoot, "tools", "GM.Development.Mcp", "bin", Configuration,
            "net10.0-windows", "GM.Development.Mcp.exe");
    }
}
