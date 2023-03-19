## 1.2.0

### Backward incompatible changes:
- IRetryBuilder.ShouldSatisfyFor renamed to IRetryBuilder.ShouldSatisfyDuring and IRetryBuilder.ShouldSatisfyTimes

### Other changes
- Added extensions to retry Func<Task> and not only Func<Task<T>>