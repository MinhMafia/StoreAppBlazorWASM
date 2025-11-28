using System;
using System.Collections.Generic;

namespace StoreApp.Services
{
    public class ValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException()
            : base("One or more validation errors occurred.")
        {
            Errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        }

        public ValidationException(IDictionary<string, string[]> errors)
            : this()
        {
            foreach (var kv in errors)
                Errors[kv.Key] = kv.Value;
        }

        public void AddError(string key, params string[] messages)
        {
            Errors[key] = messages;
        }

        public bool HasErrors => Errors.Count > 0;
    }
}
