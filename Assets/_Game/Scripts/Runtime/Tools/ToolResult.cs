namespace Farm.Tools
{
    public readonly struct ToolResult
    {
        public bool Success { get; }
        public bool PassedOut { get; }

        public ToolResult(bool success, bool passedOut)
        {
            Success = success;
            PassedOut = passedOut;
        }
    }
}
