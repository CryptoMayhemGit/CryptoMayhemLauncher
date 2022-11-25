using System;

namespace Mayhem.Util.Exceptions
{
    public class InternalException : Exception
    {
        public ValidationMessage ValidationMessage { get; set; }

        public InternalException(ValidationMessage validationMessage)
        {
            ValidationMessage = validationMessage;
        }
    }
}
