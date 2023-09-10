namespace Dn;

public static class Build
{
    public abstract record Result
    {
        private Result() { }

        public record Success : Result
        {
            private Success() { }
            public static readonly Success Instance = new();
        }
    }

    public static Result Run()
    {
        return Result.Success.Instance;
    }
}
