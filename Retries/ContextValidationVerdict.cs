namespace Zagidziran.Retries
{
    internal enum ContextValidationVerdict
    {
        Passed,
        FailedButOkay,
        Timeout,
        NoMoreRetries,
        NeedThrow,
        NeedSatisfy,
        NeedRetry,
    }
}
