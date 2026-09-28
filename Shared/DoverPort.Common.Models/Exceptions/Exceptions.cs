namespace DoverPort.Common.Models.Exceptions
{
    public class AppException : Exception
    {
        public AppException(string message) : base(message) { }
    }

    /// <summary>
    /// The contract between the pipeline's exception behaviour and the API exception filter: the
    /// behaviour logs once and rewraps, the filter unwraps and does not log again.
    /// </summary>
    public class AlreadyLoggedException : Exception
    {
        public AlreadyLoggedException(Exception inner) : base(inner.Message, inner) { }
    }

    public class AlreadyExistsException : Exception
    {
        public AlreadyExistsException(object entityId) : base($"Entity with Id: '{entityId}' already exists") { }
        public AlreadyExistsException(string message) : base(message) { }
    }

    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(object entityId)
            : base($"Entity with Id: '{entityId}' could not be found") { }

        public EntityNotFoundException(string entityName, object entityId)
            : base($"Entity '{entityName}' with Id: '{entityId}' could not be found") { }

        public EntityNotFoundException(string entityName, string parameterName, object parameterValue)
            : base($"Entity '{entityName}' with searched by '{parameterName}'with value '{parameterValue}' could not be found") { }
    }

    public class ObjectNotFoundException : Exception
    {
        public ObjectNotFoundException(string objectName, object objectValue)
            : base($"Object '{objectName}' with value: '{objectValue}' could not be found") { }

        public ObjectNotFoundException(Type objectType, object objectValue)
            : base($"Object '{objectType.Name}' with value: '{objectValue}' could not be found") { }
    }

    public class BusinessRuleValidationException(IBusinessRule brokenRule) : Exception(brokenRule.Message)
    {
        public IBusinessRule BrokenRule { get; } = brokenRule;
        public string Details { get; } = brokenRule.Message;

        public override string ToString() => $"{BrokenRule.GetType().FullName}: {BrokenRule.Message}";
    }
}
