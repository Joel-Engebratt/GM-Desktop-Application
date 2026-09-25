namespace GM.Development.Mcp
{
    public sealed record DevelopmentOptions(string RepositoryRoot, string Configuration)
    {
        public const string ExecutableName = "GM Desktop Application.exe";
        public string BuildDirectory => Path.Combine(RepositoryRoot, "GM Desktop Application",
            "GM Desktop Application", "bin", Configuration, "net10.0-windows");
        public string SessionsDirectory => Path.Combine(RepositoryRoot, "artifacts", "mcp");

        public static DevelopmentOptions Parse(string[] args)
        {
            string? root = null;
            var configuration = "Release";
            for (var index = 0; index < args.Length; index += 2)
            {
                if (index + 1 == args.Length) throw new ArgumentException("Every option requires a value.");
                switch (args[index])
                {
                    case "--repo-root": root = Path.GetFullPath(args[index + 1]); break;
                    case "--configuration": configuration = args[index + 1]; break;
                    default: throw new ArgumentException($"Unknown option: {args[index]}");
                }
            }
            if (root is null || !File.Exists(Path.Combine(root, "GM Desktop Application", "GM Desktop Application.slnx")))
                throw new ArgumentException("Pass --repo-root with the GM Application repository directory.");
            if (configuration is not ("Debug" or "Release"))
                throw new ArgumentException("Configuration must be Debug or Release.");
            return new DevelopmentOptions(root, configuration);
        }
    }
}
