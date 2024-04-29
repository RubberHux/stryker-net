namespace Stryker.Core.Options.Inputs
{
    public class UnityPathInput : Input<string>
    {
        public override string Default => string.Empty;

        protected override string Description => "Path to the version of Unity to run tests with (if Unity project)";

        public string Validate() => SuppliedInput ?? Default;
    }
}

