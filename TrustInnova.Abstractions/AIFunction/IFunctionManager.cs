namespace TrustInnova.Abstractions
{
    public interface IFunctionManager
    {
        public List<FunctionInfo> FunctionInfos { get; }
        public void AddFunction(Type cls, string name, string? customName = null, object?[]? clsArgs = null);
        public FunctionMetaInfo GetFnctionMetaInfo(string name);
    }
}
